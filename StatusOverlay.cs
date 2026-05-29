using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Deekta;

/// <summary>
/// A small, modern always-on-top status banner shown near the bottom of the primary screen
/// while recording / transcribing. Dark rounded card with a coloured accent bar on the left
/// that conveys state, the status text, and a clickable "⚙ Settings" link on the right.
///
/// It never takes focus (WS_EX_NOACTIVATE + ShowWithoutActivation), so the user's target app
/// stays foreground and typing lands where the caret is. Clicks still reach the link.
/// </summary>
internal sealed class StatusOverlay : Form
{
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_TOPMOST = 0x00000008;

    private static readonly Color CardColor = Color.FromArgb(32, 33, 38);

    private readonly Panel _accent = new();
    private readonly Label _label = new();
    private readonly LinkLabel _settingsLink = new();
    private readonly System.Windows.Forms.Timer _autoHide = new();

    public event EventHandler? SettingsClicked;

    public StatusOverlay()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = CardColor;
        Size = new Size(404, 56);
        Opacity = 0.97;
        Padding = new Padding(0);

        // Left accent bar — its colour reflects the current state.
        _accent.Dock = DockStyle.Left;
        _accent.Width = 5;
        _accent.BackColor = Color.Gray;

        // Clickable Settings link on the right.
        _settingsLink.Dock = DockStyle.Right;
        _settingsLink.Width = 116;
        _settingsLink.TextAlign = ContentAlignment.MiddleCenter;
        _settingsLink.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
        _settingsLink.LinkColor = Color.FromArgb(150, 180, 230);
        _settingsLink.ActiveLinkColor = Color.White;
        _settingsLink.LinkBehavior = LinkBehavior.HoverUnderline;
        _settingsLink.Text = Localization.Get(Tr.OpenSettingsLink);
        _settingsLink.Links.Add(0, _settingsLink.Text.Length);
        _settingsLink.LinkClicked += (_, _) => SettingsClicked?.Invoke(this, EventArgs.Empty);

        // Status text fills the middle. (Docked siblings added before the Fill control.)
        _label.Dock = DockStyle.Fill;
        _label.TextAlign = ContentAlignment.MiddleLeft;
        _label.ForeColor = Color.White;
        _label.Font = new Font("Segoe UI Semibold", 11f, FontStyle.Regular);
        _label.Padding = new Padding(14, 0, 0, 0);

        Controls.Add(_label);
        Controls.Add(_settingsLink);
        Controls.Add(_accent);

        ApplyRoundedRegion(14);

        _autoHide.Tick += (_, _) =>
        {
            _autoHide.Stop();
            Hide();
        };
    }

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
    public void FlashStatus(string text, Color accent, int ms = 1400)
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
        _accent.BackColor = accent;
        PositionBottomCentre();
        if (!Visible)
        {
            Show();
        }
        // No Activate()/BringToFront(): that could steal focus and break direct typing.
    }

    private void PositionBottomCentre()
    {
        Rectangle wa = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
        Location = new Point(
            wa.Left + (wa.Width - Width) / 2,
            wa.Bottom - Height - 64);
    }

    private void ApplyRoundedRegion(int radius)
    {
        int w = Width, h = Height, d = radius * 2;
        using var path = new GraphicsPath();
        path.AddArc(0, 0, d, d, 180, 90);
        path.AddArc(w - d, 0, d, d, 270, 90);
        path.AddArc(w - d, h - d, d, d, 0, 90);
        path.AddArc(0, h - d, d, d, 90, 90);
        path.CloseFigure();
        Region = new Region(path);
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
