using System.Globalization;
using System.Runtime.InteropServices;

namespace Deekta;

/// <summary>
/// Resolves the keyboard input language of the foreground window — the same "CA / IT / EN"
/// indicator shown in the Windows taskbar. The two-letter ISO code (e.g. "ca", "it") is sent
/// to OpenAI as the transcription language hint, so dictation follows whatever layout the
/// user is currently typing in rather than a fixed language.
/// </summary>
internal static class InputLanguageDetector
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint idThread);

    /// <summary>
    /// Returns the active layout's ISO-639-1 code (e.g. "ca"), or null if it can't be
    /// resolved — in which case the caller omits the hint and lets the model auto-detect.
    /// </summary>
    public static string? GetForegroundLanguage()
    {
        try
        {
            IntPtr hwnd = GetForegroundWindow();
            uint threadId = hwnd == IntPtr.Zero ? 0 : GetWindowThreadProcessId(hwnd, out _);

            // Low word of the HKL is the language identifier (LCID).
            IntPtr hkl = GetKeyboardLayout(threadId);
            int langId = (int)((long)hkl & 0xFFFF);
            if (langId == 0)
            {
                return null;
            }

            var culture = new CultureInfo(langId);
            string iso = culture.TwoLetterISOLanguageName;
            return string.IsNullOrWhiteSpace(iso) || iso == "iv" ? null : iso;
        }
        catch (Exception ex)
        {
            Logger.Warn($"Could not resolve foreground keyboard language: {ex.Message}");
            return null;
        }
    }
}
