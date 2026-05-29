namespace Deekta;

/// <summary>
/// User-configurable settings, persisted as JSON in %AppData%\deekta\settings.json.
/// The API key is NOT stored here in plain text — see <see cref="SettingsStore"/>, which
/// keeps it in a separate DPAPI-protected field.
/// </summary>
internal sealed class Settings
{
    public const string DefaultModel = "gpt-4o-mini-transcribe";

    /// <summary>Transcription model sent to OpenAI.</summary>
    public string Model { get; set; } = DefaultModel;

    // Note: the transcription language is detected automatically at recording time from the
    // active window's keyboard input language (see InputLanguageDetector) — not stored here.

    /// <summary>Global hotkey that toggles recording.</summary>
    public HotkeyCombo Hotkey { get; set; } = HotkeyCombo.Default;

    /// <summary>When true, paste the transcript with Ctrl+V; otherwise only copy to clipboard.</summary>
    public bool AutoPaste { get; set; } = true;

    /// <summary>Launch deekta when the user signs in (HKCU Run key).</summary>
    public bool StartWithWindows { get; set; }

    /// <summary>Play a short beep at the start and end of recording.</summary>
    public bool Beep { get; set; } = true;
}
