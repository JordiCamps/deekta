using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Deekta;

/// <summary>
/// Delivers transcribed text to the foreground application.
///
/// Primary method (auto-insert enabled): type the text directly via SendInput with
/// KEYEVENTF_UNICODE — the same approach AutoHotkey uses. This injects the characters as if
/// typed, so it does not depend on the target app honouring a Ctrl+V paste shortcut and it
/// leaves the clipboard untouched. If Windows blocks the injection (an elevated foreground
/// window, UIPI), we fall back to placing the text on the clipboard for a manual Ctrl+V.
/// </summary>
internal enum DeliveryResult
{
    /// <summary>Auto-insert disabled: text was copied to the clipboard only.</summary>
    ClipboardOnly,
    /// <summary>Text was typed directly into the foreground window.</summary>
    Pasted,
    /// <summary>Injection was blocked (e.g. an elevated window); text is on the clipboard.</summary>
    PasteBlocked,
}

internal static class TextInjector
{
    public static DeliveryResult Deliver(string text, bool autoInsert)
    {
        if (string.IsNullOrEmpty(text))
        {
            return DeliveryResult.ClipboardOnly;
        }

        if (!autoInsert)
        {
            CopyToClipboard(text);
            return DeliveryResult.ClipboardOnly;
        }

        // Type the text directly into wherever the caret is.
        if (TypeText(text))
        {
            return DeliveryResult.Pasted;
        }

        // Injection blocked: leave the text on the clipboard so the user can paste manually.
        CopyToClipboard(text);
        return DeliveryResult.PasteBlocked;
    }

    private static void CopyToClipboard(string text)
    {
        // Clipboard access requires an STA thread; the WinForms UI thread qualifies.
        // Retry a few times because other apps may briefly hold the clipboard open.
        for (int attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                Clipboard.SetText(text);
                return;
            }
            catch (ExternalException)
            {
                Thread.Sleep(40);
            }
        }
        Logger.Warn("Could not place text on the clipboard after several attempts.");
    }

    // ---- SendInput (direct Unicode typing) ----------------------------------

    private const ushort VK_RETURN = 0x0D;
    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_UNICODE = 0x0004;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    // The union must contain the LARGEST member (MOUSEINPUT) so Marshal.SizeOf<INPUT>()
    // matches the real Win32 INPUT size (40 bytes on x64). If it only held KEYBDINPUT the
    // computed cbSize would be too small and SendInput would reject every call (return 0).
    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    /// <summary>
    /// Types <paramref name="text"/> into the foreground window using Unicode key events.
    /// Returns false if SendInput injected fewer events than requested (typically blocked by
    /// UIPI because the foreground window is elevated).
    /// </summary>
    private static bool TypeText(string text)
    {
        var inputs = new List<INPUT>(text.Length * 2);
        foreach (char c in text)
        {
            switch (c)
            {
                case '\r':
                    continue; // handled together with '\n'
                case '\n':
                    inputs.Add(KeyVirtual(VK_RETURN, down: true));
                    inputs.Add(KeyVirtual(VK_RETURN, down: false));
                    break;
                default:
                    inputs.Add(KeyUnicode(c, down: true));
                    inputs.Add(KeyUnicode(c, down: false));
                    break;
            }
        }

        if (inputs.Count == 0)
        {
            return true;
        }

        INPUT[] array = inputs.ToArray();
        uint sent = SendInput((uint)array.Length, array, Marshal.SizeOf<INPUT>());
        if (sent != array.Length)
        {
            // Usually UIPI: the foreground window runs at a higher integrity level (e.g. an
            // elevated/admin terminal) than this process, so synthetic input is blocked.
            Logger.Warn($"SendInput typed {sent}/{array.Length} events (likely an elevated window).");
            return false;
        }
        return true;
    }

    private static INPUT KeyUnicode(char c, bool down) => new()
    {
        type = INPUT_KEYBOARD,
        u = new InputUnion
        {
            ki = new KEYBDINPUT
            {
                wVk = 0,
                wScan = c,
                dwFlags = KEYEVENTF_UNICODE | (down ? 0u : KEYEVENTF_KEYUP),
            },
        },
    };

    private static INPUT KeyVirtual(ushort vk, bool down) => new()
    {
        type = INPUT_KEYBOARD,
        u = new InputUnion
        {
            ki = new KEYBDINPUT
            {
                wVk = vk,
                dwFlags = down ? 0u : KEYEVENTF_KEYUP,
            },
        },
    };
}
