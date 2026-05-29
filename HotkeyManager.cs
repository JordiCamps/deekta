using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Deekta;

/// <summary>
/// Registers a system-wide hotkey via Win32 RegisterHotKey and raises
/// <see cref="HotkeyPressed"/> when it fires. A hidden <see cref="NativeWindow"/> provides
/// the window handle RegisterHotKey requires and receives the WM_HOTKEY message.
/// </summary>
internal sealed class HotkeyManager : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int HotkeyId = 0xB001; // arbitrary, app-unique within this process

    // Win32 modifier flags for RegisterHotKey.
    [Flags]
    private enum Mod : uint
    {
        Alt = 0x0001,
        Control = 0x0002,
        Shift = 0x0004,
        Win = 0x0008,
        NoRepeat = 0x4000,
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly MessageWindow _window;
    private bool _registered;

    public event EventHandler? HotkeyPressed;

    public HotkeyManager()
    {
        _window = new MessageWindow(OnHotkeyMessage);
    }

    /// <summary>
    /// Registers <paramref name="combo"/> as the active global hotkey, replacing any prior one.
    /// Returns false if the OS refused (e.g. the combination is already taken).
    /// </summary>
    public bool Register(HotkeyCombo combo)
    {
        Unregister();

        if (combo is null || !combo.IsValid)
        {
            Logger.Warn("Ignoring invalid hotkey combo.");
            return false;
        }

        Mod modifiers = Mod.NoRepeat;
        if (combo.Control) modifiers |= Mod.Control;
        if (combo.Alt) modifiers |= Mod.Alt;
        if (combo.Shift) modifiers |= Mod.Shift;
        if (combo.Win) modifiers |= Mod.Win;

        uint vk = (uint)combo.Key;
        _registered = RegisterHotKey(_window.Handle, HotkeyId, (uint)modifiers, vk);

        if (!_registered)
        {
            int err = Marshal.GetLastWin32Error();
            Logger.Warn($"RegisterHotKey failed for '{combo}' (Win32 error {err}).");
        }
        else
        {
            Logger.Info($"Hotkey registered: {combo}");
        }

        return _registered;
    }

    public void Unregister()
    {
        if (_registered)
        {
            UnregisterHotKey(_window.Handle, HotkeyId);
            _registered = false;
        }
    }

    private void OnHotkeyMessage(int id)
    {
        if (id == HotkeyId)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        Unregister();
        _window.DestroyHandle();
    }

    /// <summary>Message-only-style hidden window that forwards WM_HOTKEY to a callback.</summary>
    private sealed class MessageWindow : NativeWindow
    {
        private readonly Action<int> _onHotkey;

        public MessageWindow(Action<int> onHotkey)
        {
            _onHotkey = onHotkey;
            CreateHandle(new CreateParams());
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                _onHotkey((int)m.WParam);
            }
            base.WndProc(ref m);
        }
    }
}
