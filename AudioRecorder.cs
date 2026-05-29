using NAudio.Wave;

namespace Deekta;

/// <summary>
/// Captures the default microphone to a 16 kHz / mono / 16-bit PCM WAV file under
/// %TEMP%\deekta. Recording auto-stops after <see cref="MaxRecordingSeconds"/>.
/// </summary>
internal sealed class AudioRecorder : IDisposable
{
    public const int MaxRecordingSeconds = 120;

    // 16 kHz mono is ample for speech and keeps uploads small.
    private static readonly WaveFormat Format = new(16000, 16, 1);

    private WaveInEvent? _waveIn;
    private WaveFileWriter? _writer;
    private System.Threading.Timer? _maxTimer;
    private string? _filePath;
    private DateTime _startedAt;

    // WaveInEvent marshals RecordingStopped back to the thread that called StartRecording
    // (here the UI thread, via its SynchronizationContext). StopAsync awaits this TCS — it
    // must NOT block the UI thread, or the posted RecordingStopped callback can never run.
    private TaskCompletionSource<bool>? _stopTcs;

    public bool IsRecording { get; private set; }

    /// <summary>Raised (once) when the max-duration timer auto-stops recording.</summary>
    public event EventHandler? MaxDurationReached;

    /// <summary>Begins capturing. Throws if no input device is available.</summary>
    public void Start()
    {
        if (IsRecording)
        {
            return;
        }

        if (WaveInEvent.DeviceCount == 0)
        {
            throw new InvalidOperationException("No microphone input device was found.");
        }

        _stopTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _filePath = Path.Combine(AppPaths.TempAudioDir, $"rec_{Guid.NewGuid():N}.wav");
        _waveIn = new WaveInEvent { WaveFormat = Format, DeviceNumber = 0 };
        _writer = new WaveFileWriter(_filePath, Format);

        _waveIn.DataAvailable += OnDataAvailable;
        _waveIn.RecordingStopped += OnRecordingStopped;

        _startedAt = DateTime.UtcNow;
        IsRecording = true;
        _waveIn.StartRecording();

        _maxTimer = new System.Threading.Timer(
            _ => OnMaxDuration(), null,
            TimeSpan.FromSeconds(MaxRecordingSeconds), Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// Stops capturing and returns the recorded file path and its duration once the WAV file
    /// has been flushed and closed (so it is safe to read). Returns (null, zero) if nothing
    /// was being recorded. Awaiting (rather than blocking) lets RecordingStopped run on the UI thread.
    /// </summary>
    public async Task<(string? FilePath, TimeSpan Duration)> StopAsync()
    {
        if (!IsRecording)
        {
            return (null, TimeSpan.Zero);
        }

        IsRecording = false;
        _maxTimer?.Dispose();
        _maxTimer = null;

        var duration = DateTime.UtcNow - _startedAt;
        Task<bool> stopped = _stopTcs?.Task ?? Task.FromResult(true);

        // RecordingStopped (which flushes/closes the writer) is posted to the UI thread.
        _waveIn?.StopRecording();

        // Yield the UI thread so that posted callback can run; guard with a timeout.
        if (await Task.WhenAny(stopped, Task.Delay(TimeSpan.FromSeconds(5))).ConfigureAwait(true) != stopped)
        {
            Logger.Warn("Timed out waiting for the recorder to finalise the WAV file.");
        }

        string? path = _filePath;
        _filePath = null;
        return (path, duration);
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        try
        {
            _writer?.Write(e.Buffer, 0, e.BytesRecorded);
        }
        catch (Exception ex)
        {
            Logger.Error("Error writing audio buffer.", ex);
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        try
        {
            _writer?.Flush();
            _writer?.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Error("Error finalising WAV file.", ex);
        }
        finally
        {
            _writer = null;
            _waveIn?.Dispose();
            _waveIn = null;
            // File handle is now released; unblock StopAsync().
            _stopTcs?.TrySetResult(true);
        }

        if (e.Exception is not null)
        {
            Logger.Error("Recording stopped with error.", e.Exception);
        }
    }

    private void OnMaxDuration()
    {
        if (IsRecording)
        {
            MaxDurationReached?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        _maxTimer?.Dispose();
        try { _waveIn?.Dispose(); } catch { /* ignore */ }
        try { _writer?.Dispose(); } catch { /* ignore */ }
        _stopTcs?.TrySetResult(true);
    }
}
