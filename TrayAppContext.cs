using System.Drawing;
using System.Media;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Deekta;

/// <summary>
/// The application's heart: owns the tray icon and menu, the global hotkey, and the
/// Idle ↔ Recording ↔ Transcribing state machine that ties recording, OpenAI transcription
/// and text delivery together.
/// </summary>
internal sealed class TrayAppContext : ApplicationContext
{
    private const int MinClipMilliseconds = 1000;

    private readonly SettingsStore _store;
    private readonly HotkeyManager _hotkey = new();
    private readonly AudioRecorder _recorder = new();
    private readonly OpenAiClient _client = new();

    private readonly NotifyIcon _trayIcon;
    private readonly Icon _idleIcon;
    private readonly Icon _recordingIcon;
    private readonly ToolStripMenuItem _startWithWindowsItem;
    private readonly StatusOverlay _overlay = new();

    // Cross-thread → UI-thread marshalling for the "show settings" signal and the
    // max-duration timer. The hidden window's WndProc runs on the UI thread.
    private readonly MarshalWindow _marshalWindow;
    private readonly EventWaitHandle _showSettingsSignal;
    private readonly RegisteredWaitHandle _showSettingsWait;

    private SynchronizationContext? _uiContext;
    private SettingsForm? _settingsForm;
    private string? _captureLanguage;
    private bool _isRecording;
    private bool _isBusy;
    private bool _disposed;

    public TrayAppContext()
    {
        _store = new SettingsStore().Load();

        // Keep the persisted "start with Windows" flag in sync with the actual registry state.
        _store.Settings.StartWithWindows = StartupManager.IsEnabled();

        CleanupOrphanTempFiles();

        _idleIcon = TrayIconFactory.CreateIdle();
        _recordingIcon = TrayIconFactory.CreateRecording();

        _startWithWindowsItem = new ToolStripMenuItem(Localization.Get(Tr.MenuStartup))
        {
            CheckOnClick = true,
            Checked = _store.Settings.StartWithWindows,
        };
        _startWithWindowsItem.Click += OnToggleStartWithWindows;

        var menu = new ContextMenuStrip();
        var settingsItem = new ToolStripMenuItem(Localization.Get(Tr.MenuSettings));
        settingsItem.Click += (_, _) => OpenSettings();
        var exitItem = new ToolStripMenuItem(Localization.Get(Tr.MenuExit));
        exitItem.Click += (_, _) => ExitApp();

        menu.Items.Add(settingsItem);
        menu.Items.Add(_startWithWindowsItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        _trayIcon = new NotifyIcon
        {
            Icon = _idleIcon,
            Visible = true,
            Text = Localization.Get(Tr.TipIdle),
            ContextMenuStrip = menu,
        };
        _trayIcon.DoubleClick += (_, _) => OpenSettings();
        // Clicking the notification popup opens Settings (a "link" to configuration).
        _trayIcon.BalloonTipClicked += (_, _) => OpenSettings();

        _hotkey.HotkeyPressed += OnHotkeyPressed;
        _recorder.MaxDurationReached += OnMaxDurationReached;

        // Hidden window used to marshal background signals onto the UI thread.
        _marshalWindow = new MarshalWindow(OnShowSettingsRequested);

        // Wait for a second launch asking us to show Settings.
        _showSettingsSignal = SingleInstance.CreateShowSettingsSignal();
        _showSettingsWait = ThreadPool.RegisterWaitForSingleObject(
            _showSettingsSignal,
            (_, _) => _marshalWindow.PostShowSettings(),
            state: null,
            millisecondsTimeOutInterval: Timeout.Infinite,
            executeOnlyOnce: false);

        RegisterHotkey();

        // If the API key is missing, open Settings immediately. Posting the message defers
        // it until the message loop is running (we can't show a modal dialog from here yet).
        if (!_store.HasApiKey)
        {
            _marshalWindow.PostShowSettings();
        }
    }

    // ---- Hotkey -------------------------------------------------------------

    private void RegisterHotkey()
    {
        bool ok = _hotkey.Register(_store.Settings.Hotkey);
        if (!ok)
        {
            ShowBalloon(
                Localization.Get(Tr.HotkeyRegisterFailed, _store.Settings.Hotkey),
                ToolTipIcon.Warning);
        }
    }

    private void OnHotkeyPressed(object? sender, EventArgs e)
    {
        // Runs on the UI thread (via the hidden window's WndProc); capture the context so
        // the off-thread max-duration timer can marshal back here.
        _uiContext ??= SynchronizationContext.Current;

        if (_isBusy)
        {
            return; // Transcription in flight; ignore toggles.
        }

        if (!_isRecording)
        {
            StartRecording();
        }
        else
        {
            _ = StopAndTranscribeAsync();
        }
    }

    private void OnMaxDurationReached(object? sender, EventArgs e)
    {
        // Timer thread → marshal to UI thread.
        void Stop()
        {
            if (_isRecording && !_isBusy)
            {
                ShowBalloon(Localization.Get(Tr.StoppedAtLimit, AudioRecorder.MaxRecordingSeconds), ToolTipIcon.Info);
                _ = StopAndTranscribeAsync();
            }
        }

        if (_uiContext is not null)
        {
            _uiContext.Post(_ => Stop(), null);
        }
        else
        {
            Stop();
        }
    }

    // ---- Recording / transcription ------------------------------------------

    private void StartRecording()
    {
        if (!_store.HasApiKey)
        {
            ShowBalloon(Localization.Get(Tr.NeedApiKey), ToolTipIcon.Warning);
            OpenSettings();
            return;
        }

        try
        {
            // Capture the active window's keyboard language now, while it is still foreground.
            _captureLanguage = InputLanguageDetector.GetForegroundLanguage();
            _recorder.Start();
            _isRecording = true;
            SetRecordingUi(true);
            PlayBeep(start: true);
            string lang = _captureLanguage is null ? "auto" : _captureLanguage.ToUpperInvariant();
            _overlay.ShowStatus(
                $"●  {Localization.Get(Tr.Recording)} [{lang}]  ({Localization.Get(Tr.StopHint)})",
                Color.FromArgb(190, 40, 40));
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to start recording.", ex);
            _overlay.FlashStatus("✗  " + Localization.Get(Tr.MicError), Color.FromArgb(170, 40, 40), 3000);
            ShowBalloon(Localization.Get(Tr.MicError), ToolTipIcon.Error);
            _isRecording = false;
            SetRecordingUi(false);
        }
    }

    private async Task StopAndTranscribeAsync()
    {
        (string? filePath, TimeSpan duration) = await _recorder.StopAsync().ConfigureAwait(true);
        _isRecording = false;
        SetRecordingUi(false);
        PlayBeep(start: false);

        if (filePath is null)
        {
            _overlay.HideStatus();
            return;
        }

        // Too short to be meaningful: discard silently.
        if (duration.TotalMilliseconds < MinClipMilliseconds)
        {
            Logger.Info("Clip under 1s ignored.");
            _overlay.FlashStatus(Localization.Get(Tr.TooShort), Color.FromArgb(90, 90, 95));
            TryDeleteFile(filePath);
            return;
        }

        _isBusy = true;
        _trayIcon.Text = Localization.Get(Tr.TipTranscribing);
        _overlay.ShowStatus("⏳  " + Localization.Get(Tr.Transcribing), Color.FromArgb(40, 90, 170));

        try
        {
            string text = await _client.TranscribeAsync(
                filePath, _store.ApiKey, _store.Settings.Model, _captureLanguage)
                .ConfigureAwait(true); // resume on UI thread for clipboard/SendInput

            if (string.IsNullOrWhiteSpace(text))
            {
                _overlay.FlashStatus(Localization.Get(Tr.NoText), Color.FromArgb(90, 90, 95));
                ShowBalloon(Localization.Get(Tr.NoText), ToolTipIcon.Info);
            }
            else
            {
                DeliveryResult result = TextInjector.Deliver(text, _store.Settings.AutoPaste);
                switch (result)
                {
                    case DeliveryResult.Pasted:
                        _overlay.FlashStatus("✓  " + Localization.Get(Tr.TextInserted), Color.FromArgb(40, 140, 70));
                        break;
                    case DeliveryResult.ClipboardOnly:
                        _overlay.FlashStatus("✓  " + Localization.Get(Tr.CopiedClipboard), Color.FromArgb(40, 140, 70));
                        break;
                    case DeliveryResult.PasteBlocked:
                        _overlay.FlashStatus(
                            "📋  " + Localization.Get(Tr.CopiedPasteManually),
                            Color.FromArgb(180, 120, 30), 4000);
                        ShowBalloon(Localization.Get(Tr.PasteBlockedBalloon), ToolTipIcon.Warning, 4000);
                        break;
                }
            }
        }
        catch (TranscriptionException ex)
        {
            Logger.Error("Transcription failed.", ex.InnerException ?? ex);
            _overlay.FlashStatus("✗  " + ex.Message, Color.FromArgb(170, 40, 40), 3500);
            ShowBalloon(ex.Message, ToolTipIcon.Error);
        }
        catch (Exception ex)
        {
            Logger.Error("Unexpected transcription error.", ex);
            _overlay.FlashStatus("✗  " + Localization.Get(Tr.TranscribeError), Color.FromArgb(170, 40, 40), 3000);
            ShowBalloon(Localization.Get(Tr.TranscribeError), ToolTipIcon.Error);
        }
        finally
        {
            TryDeleteFile(filePath);
            _isBusy = false;
            _trayIcon.Text = "deekta";
        }
    }

    // ---- UI helpers ---------------------------------------------------------

    private void SetRecordingUi(bool recording)
    {
        _trayIcon.Icon = recording ? _recordingIcon : _idleIcon;
        _trayIcon.Text = recording ? Localization.Get(Tr.TipRecording) : Localization.Get(Tr.TipIdle);
    }

    private void PlayBeep(bool start)
    {
        if (!_store.Settings.Beep)
        {
            return;
        }

        try
        {
            // Non-blocking system sounds; distinct cue for start vs stop.
            if (start) SystemSounds.Asterisk.Play();
            else SystemSounds.Beep.Play();
        }
        catch
        {
            // Audio cue is best-effort.
        }
    }

    private void ShowBalloon(string message, ToolTipIcon icon, int timeoutMs = 3000)
    {
        try
        {
            _trayIcon.BalloonTipTitle = "deekta";
            _trayIcon.BalloonTipText = message;
            _trayIcon.BalloonTipIcon = icon;
            _trayIcon.ShowBalloonTip(timeoutMs);
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to show balloon tip.", ex);
        }
    }

    // ---- Menu actions -------------------------------------------------------

    private void OnShowSettingsRequested()
    {
        // Runs on the UI thread via MarshalWindow.WndProc.
        _uiContext ??= SynchronizationContext.Current;
        OpenSettings();
    }

    private void OpenSettings()
    {
        // Already open: just bring it to the front instead of stacking dialogs.
        if (_settingsForm is not null)
        {
            try
            {
                _settingsForm.WindowState = FormWindowState.Normal;
                _settingsForm.Activate();
                _settingsForm.BringToFront();
            }
            catch { /* form may be closing */ }
            return;
        }

        using var form = new SettingsForm(_store.Settings, _store.ApiKey);
        _settingsForm = form;
        try
        {
            if (form.ShowDialog() != DialogResult.OK)
            {
                return;
            }
        }
        finally
        {
            _settingsForm = null;
        }

        HotkeyCombo previousHotkey = _store.Settings.Hotkey;
        bool previousStartup = _store.Settings.StartWithWindows;

        _store.Settings = form.Settings;
        _store.ApiKey = form.ApiKey;

        try
        {
            _store.Save();
        }
        catch
        {
            ShowBalloon(Localization.Get(Tr.SaveSettingsError), ToolTipIcon.Error);
        }

        // Apply the start-with-Windows change to the registry if it changed.
        if (_store.Settings.StartWithWindows != previousStartup)
        {
            ApplyStartWithWindows(_store.Settings.StartWithWindows);
        }
        _startWithWindowsItem.Checked = _store.Settings.StartWithWindows;

        // Re-register the hotkey if it changed.
        if (!HotkeyEquals(previousHotkey, _store.Settings.Hotkey))
        {
            RegisterHotkey();
        }
    }

    private void OnToggleStartWithWindows(object? sender, EventArgs e)
    {
        bool enabled = _startWithWindowsItem.Checked;
        ApplyStartWithWindows(enabled);
        _store.Settings.StartWithWindows = StartupManager.IsEnabled();
        _startWithWindowsItem.Checked = _store.Settings.StartWithWindows;

        try
        {
            _store.Save();
        }
        catch
        {
            // Already logged; registry is the source of truth here.
        }
    }

    private void ApplyStartWithWindows(bool enabled)
    {
        try
        {
            StartupManager.SetEnabled(enabled);
        }
        catch
        {
            ShowBalloon(Localization.Get(Tr.StartupChangeError), ToolTipIcon.Error);
        }
    }

    private static bool HotkeyEquals(HotkeyCombo a, HotkeyCombo b) =>
        a.Control == b.Control && a.Alt == b.Alt && a.Shift == b.Shift &&
        a.Win == b.Win && a.Key == b.Key;

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Could not delete temp file: {ex.Message}");
        }
    }

    private static void CleanupOrphanTempFiles()
    {
        try
        {
            foreach (string wav in Directory.EnumerateFiles(AppPaths.TempAudioDir, "rec_*.wav"))
            {
                try { File.Delete(wav); } catch { /* in use; skip */ }
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Temp cleanup failed: {ex.Message}");
        }
    }

    // ---- Lifecycle ----------------------------------------------------------

    private void ExitApp()
    {
        _trayIcon.Visible = false;
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            // _recorder.Dispose() stops any in-progress capture; no need to await here.
            _showSettingsWait.Unregister(_showSettingsSignal);
            _showSettingsSignal.Dispose();
            _marshalWindow.DestroyHandle();
            _overlay.Dispose();
            _hotkey.Dispose();
            _recorder.Dispose();
            _client.Dispose();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _idleIcon.Dispose();
            _recordingIcon.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// Hidden window whose WndProc runs on the UI thread. Background threads call
    /// <see cref="PostShowSettings"/> to safely request the Settings dialog.
    /// </summary>
    private sealed class MarshalWindow : NativeWindow
    {
        // App-unique message id, shared by all windows in this user session.
        private static readonly uint ShowSettingsMessage =
            RegisterWindowMessage("deekta_WM_ShowSettings_{7C3E1A90}");

        private readonly Action _onShowSettings;

        public MarshalWindow(Action onShowSettings)
        {
            _onShowSettings = onShowSettings;
            CreateHandle(new CreateParams());
        }

        /// <summary>Queues a show-settings request; safe to call from any thread.</summary>
        public void PostShowSettings() =>
            PostMessage(Handle, ShowSettingsMessage, IntPtr.Zero, IntPtr.Zero);

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == ShowSettingsMessage)
            {
                _onShowSettings();
                return;
            }
            base.WndProc(ref m);
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern uint RegisterWindowMessage(string lpString);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    }
}
