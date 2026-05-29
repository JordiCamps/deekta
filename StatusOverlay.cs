using System.Drawing;
using System.Windows.Forms;

namespace Deekta;

/// <summary>
/// A small always-on-top status banner shown near the bottom of the primary screen while
/// recording / transcribing. It never takes focus (WS_EX_NOACTIVATE + ShowWithoutActivation),
/// so the user's target application stays foreground and Ctrl+V pastes where the caret is.
/// This is the primary visual feedback when tray balloons are suppressed by Windows.
/// </summary>
internal sealed class StatusOverlay : Form
{
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_TOPMOST = 0x00000008;

    private readonly Label _label = new();
    private readonly System.Windows.Forms.Timer _autoHide = new();

    public StatusOverlay()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(28, 28, 30);
        Size = new Size(300, 52);
        Opacity = 0.92;

        _label.Dock = DockStyle.Fill;
        _label.TextAlign = ContentAlignment.MiddleCenter;
        _label.ForeColor = Color.White;
        _label.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
        Controls.Add(_label);

        _autoHide.Tick += (_, _) =>
        {
            _autoHide.Stop();
            Hide();
        };
    }

    // Don't activate when shown — keep focus on the user's app.
    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST;
            return cp;
        }
    }

    /// <summary>Shows a persistent status (no auto-hide), e.g. while recording/transcribing.</summary>
    public void ShowStatus(string text, Color accent)
    {
        _autoHide.Stop();
        Render(text, accent);
    }

    /// <summary>Shows a transient status that hides itself after <paramref name="ms"/>.</summary>
    public void FlashStatus(string text, Color accent, int ms = 1200)
    {
        Render(text, accent);
        _autoHide.Stop();
        _autoHide.Interval = ms;
        _autoHide.Start();
    }

    public void HideStatus()
    {
        _autoHide.Stop();
        Hide();
    }

    private void Render(string text, Color accent)
    {
        _label.Text = text;
        BackColor = accent;
        PositionBottomCentre();
        if (!Visible)
        {
            Show();
        }
        // Note: we deliberately do NOT call Activate()/BringToFront() — that could steal focus
        // from the target window and break direct typing. TopMost handles z-order.
    }

    private void PositionBottomCentre()
    {
        Rectangle wa = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
        Location = new Point(
            wa.Left + (wa.Width - Width) / 2,
            wa.Bottom - Height - 60);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _autoHide.Dispose();
        }
        base.Dispose(disposing);
    }
}
