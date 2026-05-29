using System.Drawing;
using System.Windows.Forms;

namespace Deekta;

/// <summary>
/// Code-defined settings dialog. Edits a copy of the current settings + API key; the caller
/// reads <see cref="Settings"/> and <see cref="ApiKey"/> after a DialogResult.OK.
/// </summary>
internal sealed class SettingsForm : Form
{
    private readonly TextBox _apiKeyBox = new();
    private readonly TextBox _modelBox = new();
    private readonly TextBox _hotkeyBox = new();
    private readonly CheckBox _autoPaste = new();
    private readonly CheckBox _startWithWindows = new();
    private readonly CheckBox _beep = new();

    private readonly HotkeyCombo _hotkey;

    public Settings Settings { get; }
    public string ApiKey => _apiKeyBox.Text.Trim();

    public SettingsForm(Settings current, string currentApiKey)
    {
        // Work on a clone so cancelling discards edits.
        Settings = new Settings
        {
            Model = current.Model,
            Hotkey = new HotkeyCombo
            {
                Control = current.Hotkey.Control,
                Alt = current.Hotkey.Alt,
                Shift = current.Hotkey.Shift,
                Win = current.Hotkey.Win,
                Key = current.Hotkey.Key,
            },
            AutoPaste = current.AutoPaste,
            StartWithWindows = current.StartWithWindows,
            Beep = current.Beep,
        };
        _hotkey = Settings.Hotkey;

        BuildUi();
        LoadValues(currentApiKey);
    }

    private void BuildUi()
    {
        Text = Localization.Get(Tr.WinTitle);
        Icon = TrayIconFactory.CreateAppIcon();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        ClientSize = new Size(460, 300);
        Font = new Font("Segoe UI", 9f);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            Padding = new Padding(14),
            AutoSize = true,
            ColumnStyles =
            {
                new ColumnStyle(SizeType.Absolute, 130),
                new ColumnStyle(SizeType.Percent, 100),
            },
        };

        _apiKeyBox.UseSystemPasswordChar = true;
        _apiKeyBox.Width = 280;
        _modelBox.Width = 280;

        _hotkeyBox.Width = 280;
        _hotkeyBox.ReadOnly = true;
        _hotkeyBox.BackColor = SystemColors.Window;
        _hotkeyBox.Cursor = Cursors.Hand;
        _hotkeyBox.KeyDown += HotkeyBox_KeyDown;
        _hotkeyBox.Enter += (_, _) => _hotkeyBox.Text = Localization.Get(Tr.HotkeyPrompt);
        _hotkeyBox.Leave += (_, _) => _hotkeyBox.Text = _hotkey.ToString();

        AddRow(layout, Localization.Get(Tr.LblApiKey), _apiKeyBox);
        AddRow(layout, Localization.Get(Tr.LblModel), _modelBox);
        AddRow(layout, Localization.Get(Tr.LblLanguage), new Label
        {
            Text = Localization.Get(Tr.LangNote),
            ForeColor = SystemColors.GrayText,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill,
            AutoSize = false,
        });
        AddRow(layout, Localization.Get(Tr.LblHotkey), _hotkeyBox);
        AddRow(layout, Localization.Get(Tr.LblAutoInsert), _autoPaste);
        AddRow(layout, Localization.Get(Tr.LblStartup), _startWithWindows);
        AddRow(layout, Localization.Get(Tr.LblBeep), _beep);

        var ok = new Button { Text = Localization.Get(Tr.BtnSave), DialogResult = DialogResult.OK, Width = 90 };
        var cancel = new Button { Text = Localization.Get(Tr.BtnCancel), DialogResult = DialogResult.Cancel, Width = 90 };
        ok.Click += Ok_Click;

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(14),
            Height = 56,
        };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);

        Controls.Add(layout);
        Controls.Add(buttons);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private static void AddRow(TableLayoutPanel layout, string label, Control control)
    {
        int row = layout.RowCount;
        layout.RowCount++;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.Controls.Add(new Label
        {
            Text = label,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill,
            AutoSize = false,
        }, 0, row);
        layout.Controls.Add(control, 1, row);
    }

    private void LoadValues(string currentApiKey)
    {
        _apiKeyBox.Text = currentApiKey;
        _modelBox.Text = Settings.Model;
        _hotkeyBox.Text = _hotkey.ToString();
        _autoPaste.Checked = Settings.AutoPaste;
        _startWithWindows.Checked = Settings.StartWithWindows;
        _beep.Checked = Settings.Beep;
    }

    private void HotkeyBox_KeyDown(object? sender, KeyEventArgs e)
    {
        e.SuppressKeyPress = true;
        e.Handled = true;

        Keys key = e.KeyCode;
        // Ignore standalone modifier presses; wait for a "real" key.
        if (key is Keys.ControlKey or Keys.Menu or Keys.ShiftKey or Keys.LWin or Keys.RWin or Keys.None)
        {
            return;
        }

        _hotkey.Control = e.Control;
        _hotkey.Alt = e.Alt;
        _hotkey.Shift = e.Shift;
        _hotkey.Win = (Control.ModifierKeys & Keys.LWin) != 0 || (Control.ModifierKeys & Keys.RWin) != 0;
        _hotkey.Key = key;
        _hotkeyBox.Text = _hotkey.ToString();
    }

    private void Ok_Click(object? sender, EventArgs e)
    {
        string model = _modelBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(model))
        {
            MessageBox.Show(this, Localization.Get(Tr.ErrModelEmpty), "deekta",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        if (!_hotkey.IsValid)
        {
            MessageBox.Show(this, Localization.Get(Tr.ErrHotkeyModifier),
                "deekta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        Settings.Model = model;
        Settings.AutoPaste = _autoPaste.Checked;
        Settings.StartWithWindows = _startWithWindows.Checked;
        Settings.Beep = _beep.Checked;
        // Settings.Hotkey is _hotkey (same reference), already updated.
    }
}
