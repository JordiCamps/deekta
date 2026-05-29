using System.Windows.Forms;

namespace Deekta;

/// <summary>
/// A serialisable global-hotkey definition: a set of modifiers plus a single key.
/// Modifier values match the WinForms <see cref="Keys"/> flags; <see cref="HotkeyManager"/>
/// translates them into the Win32 MOD_* / virtual-key codes RegisterHotKey expects.
/// </summary>
internal sealed class HotkeyCombo
{
    public bool Control { get; set; } = true;
    public bool Alt { get; set; } = true;
    public bool Shift { get; set; }
    public bool Win { get; set; }
    public Keys Key { get; set; } = Keys.D;

    /// <summary>Default global hotkey: Ctrl + Alt + D.</summary>
    public static HotkeyCombo Default => new();

    public bool IsValid => Key != Keys.None && (Control || Alt || Shift || Win);

    public override string ToString()
    {
        var parts = new List<string>();
        if (Control) parts.Add("Ctrl");
        if (Alt) parts.Add("Alt");
        if (Shift) parts.Add("Shift");
        if (Win) parts.Add("Win");
        parts.Add(Key.ToString());
        return string.Join(" + ", parts);
    }
}
