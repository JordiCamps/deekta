using System.Drawing;
using System.Windows.Forms;

namespace Deekta;

/// <summary>
/// Settings dialog. Grouped, friendlier layout:
/// - OpenAI account: the key is shown masked once set; editing requires clicking "Change…".
/// - Model: a dropdown with a short description of each transcription model.
/// - Shortcut: modifier check boxes + a key dropdown (no "press the combination" capture).
/// - Options: clearly labelled toggles with one-line hints.
/// Edits a clone of the settings; the caller reads <see cref="Settings"/>/<see cref="ApiKey"/> on OK.
/// </summary>
internal sealed class SettingsForm : Form
{
    private static readonly (string Id, Tr Desc)[] ModelChoices =
    {
        ("gpt-4o-mini-transcribe", Tr.ModelMiniDesc),
        ("gpt-4o-transcribe", Tr.ModelFullDesc),
        ("whisper-1", Tr.ModelWhisperDesc),
    };

    // API key
    private readonly Label _keySummary = new();
    private readonly Button _keyChange = new();
    private readonly TextBox _keyBox = new();
    private readonly Label _keyHint = new();
    private bool _editingKey;
    private readonly string _originalApiKey;

    // Model
    private readonly ComboBox _modelCombo = new();
    private readonly Label _modelDesc = new();

    // Shortcut
    private readonly CheckBox _ctrl = new() { Text = "Ctrl" };
    private readonly CheckBox _alt = new() { Text = "Alt" };
    private readonly CheckBox _shift = new() { Text = "Shift" };
    private readonly CheckBox _win = new() { Text = "Win" };
    private readonly ComboBox _keyCombo = new();
    private readonly Label _shortcutPreview = new();

    // Options
    private readonly CheckBox _autoInsert = new();
    private readonly CheckBox _startup = new();
    private readonly CheckBox _beep = new();

    private readonly HotkeyCombo _hotkey;

    public Settings Settings { get; }
    public string ApiKey => _editingKey ? _keyBox.Text.Trim() : _originalApiKey;

    public SettingsForm(Settings current, string currentApiKey)
    {
        _originalApiKey = currentApiKey ?? string.Empty;
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
        LoadValues();
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
        Font = new Font("Segoe UI", 9f);
        BackColor = Color.White;
        ClientSize = new Size(484, 612);

        var intro = new Label
        {
            Text = Localization.Get(Tr.SettingsIntro),
            Location = new Point(14, 10),
            Size = new Size(458, 44),
            AutoSize = false,
            ForeColor = Color.FromArgb(90, 90, 95),
        };
        Controls.Add(intro);

        BuildApiGroup(top: 60);
        BuildModelGroup(top: 166);
        BuildShortcutGroup(top: 270);
        BuildOptionsGroup(top: 384);
        BuildButtons(top: 566);
    }

    private GroupBox Group(Tr title, int top, int height)
    {
        var g = new GroupBox
        {
            Text = "  " + Localization.Get(title) + "  ",
            Location = new Point(12, top),
            Size = new Size(460, height),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
        };
        Controls.Add(g);
        return g;
    }

    private static Label Hint(string text, int x, int y, int width) => new()
    {
        Text = text,
        Location = new Point(x, y),
        Size = new Size(width, 18),
        ForeColor = Color.FromArgb(120, 120, 125),
        Font = new Font("Segoe UI", 8.25f, FontStyle.Regular),
    };

    private void BuildApiGroup(int top)
    {
        GroupBox g = Group(Tr.GrpApi, top, 98);

        _keySummary.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
        _keySummary.ForeColor = Color.FromArgb(30, 120, 60);
        _keySummary.Location = new Point(14, 28);
        _keySummary.AutoSize = true;

        _keyChange.Text = Localization.Get(Tr.BtnChange);
        _keyChange.Size = new Size(96, 26);
        _keyChange.Location = new Point(346, 24);
        _keyChange.Click += (_, _) => SetKeyEditing(true);

        _keyBox.UseSystemPasswordChar = true;
        _keyBox.Location = new Point(14, 26);
        _keyBox.Width = 430;
        _keyBox.Font = new Font("Segoe UI", 9.5f);

        _keyHint.Text = Localization.Get(Tr.ApiKeyPlaceholder) + "   ·   " + Localization.Get(Tr.ApiKeyOnlyOpenAi);
        _keyHint.Location = new Point(14, 56);
        _keyHint.Size = new Size(432, 32);
        _keyHint.ForeColor = Color.FromArgb(120, 120, 125);
        _keyHint.Font = new Font("Segoe UI", 8.25f);

        g.Controls.Add(_keySummary);
        g.Controls.Add(_keyChange);
        g.Controls.Add(_keyBox);
        g.Controls.Add(_keyHint);
    }

    private void BuildModelGroup(int top)
    {
        GroupBox g = Group(Tr.GrpModel, top, 96);

        _modelCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _modelCombo.Location = new Point(14, 28);
        _modelCombo.Width = 430;
        _modelCombo.Font = new Font("Segoe UI", 9.5f);
        foreach ((string id, Tr _) in ModelChoices)
        {
            _modelCombo.Items.Add(id);
        }
        _modelCombo.SelectedIndexChanged += (_, _) => UpdateModelDesc();

        _modelDesc.Location = new Point(16, 58);
        _modelDesc.Size = new Size(428, 30);
        _modelDesc.ForeColor = Color.FromArgb(120, 120, 125);
        _modelDesc.Font = new Font("Segoe UI", 8.5f);

        g.Controls.Add(_modelCombo);
        g.Controls.Add(_modelDesc);
    }

    private void BuildShortcutGroup(int top)
    {
        GroupBox g = Group(Tr.GrpShortcut, top, 106);

        _ctrl.Location = new Point(14, 26); _ctrl.AutoSize = true;
        _alt.Location = new Point(96, 26); _alt.AutoSize = true;
        _shift.Location = new Point(168, 26); _shift.AutoSize = true;
        _win.Location = new Point(252, 26); _win.AutoSize = true;
        foreach (CheckBox c in new[] { _ctrl, _alt, _shift, _win })
        {
            c.Font = new Font("Segoe UI", 9.5f);
            c.CheckedChanged += (_, _) => UpdateShortcutPreview();
            g.Controls.Add(c);
        }

        var keyLabel = new Label
        {
            Text = Localization.Get(Tr.LblKeyChar),
            Location = new Point(14, 58),
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5f),
        };
        _keyCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _keyCombo.Location = new Point(64, 54);
        _keyCombo.Width = 120;
        _keyCombo.Font = new Font("Segoe UI", 9.5f);
        PopulateKeyCombo();
        _keyCombo.SelectedIndexChanged += (_, _) => UpdateShortcutPreview();

        _shortcutPreview.Location = new Point(200, 58);
        _shortcutPreview.Size = new Size(244, 22);
        _shortcutPreview.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        _shortcutPreview.ForeColor = Color.FromArgb(40, 90, 170);

        g.Controls.Add(keyLabel);
        g.Controls.Add(_keyCombo);
        g.Controls.Add(_shortcutPreview);
    }

    private void BuildOptionsGroup(int top)
    {
        GroupBox g = Group(Tr.GrpOptions, top, 168);

        _autoInsert.Text = Localization.Get(Tr.LblAutoInsert);
        _autoInsert.Location = new Point(14, 24); _autoInsert.AutoSize = true;
        _autoInsert.Font = new Font("Segoe UI", 9.5f);

        _startup.Text = Localization.Get(Tr.LblStartup);
        _startup.Location = new Point(14, 68); _startup.AutoSize = true;
        _startup.Font = new Font("Segoe UI", 9.5f);

        _beep.Text = Localization.Get(Tr.LblBeep);
        _beep.Location = new Point(14, 112); _beep.AutoSize = true;
        _beep.Font = new Font("Segoe UI", 9.5f);

        g.Controls.Add(_autoInsert);
        g.Controls.Add(Hint(Localization.Get(Tr.AutoInsertHint), 34, 44, 420));
        g.Controls.Add(_startup);
        g.Controls.Add(Hint(Localization.Get(Tr.StartupHint), 34, 88, 420));
        g.Controls.Add(_beep);
        g.Controls.Add(Hint(Localization.Get(Tr.BeepHint), 34, 132, 420));
        g.Controls.Add(new Label
        {
            Text = $"{Localization.Get(Tr.LblLanguage)}: {Localization.Get(Tr.LangNote)}",
            Location = new Point(14, 148),
            Size = new Size(432, 16),
            ForeColor = Color.FromArgb(150, 150, 155),
            Font = new Font("Segoe UI", 8f, FontStyle.Italic),
        });
    }

    private void BuildButtons(int top)
    {
        var save = new Button
        {
            Text = Localization.Get(Tr.BtnSave),
            DialogResult = DialogResult.OK,
            Size = new Size(100, 30),
            Location = new Point(268, top),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
        };
        save.Click += Save_Click;
        var cancel = new Button
        {
            Text = Localization.Get(Tr.BtnCancel),
            DialogResult = DialogResult.Cancel,
            Size = new Size(100, 30),
            Location = new Point(374, top),
        };

        Controls.Add(save);
        Controls.Add(cancel);
        AcceptButton = save;
        CancelButton = cancel;
    }

    // ---- Values ------------------------------------------------------------

    private void LoadValues()
    {
        SelectModel(Settings.Model);
        _ctrl.Checked = _hotkey.Control;
        _alt.Checked = _hotkey.Alt;
        _shift.Checked = _hotkey.Shift;
        _win.Checked = _hotkey.Win;
        SelectKey(_hotkey.Key);
        _autoInsert.Checked = Settings.AutoPaste;
        _startup.Checked = Settings.StartWithWindows;
        _beep.Checked = Settings.Beep;

        SetKeyEditing(string.IsNullOrEmpty(_originalApiKey));
        UpdateShortcutPreview();
    }

    private void SetKeyEditing(bool editing)
    {
        _editingKey = editing;
        _keyBox.Visible = editing;
        _keyHint.Visible = editing;
        _keySummary.Visible = !editing;
        _keyChange.Visible = !editing;

        if (!editing)
        {
            _keySummary.Text = $"{Localization.Get(Tr.ApiKeyConfigured)}   ({Mask(_originalApiKey)})";
        }
        else
        {
            _keyBox.Text = string.Empty;
            _keyBox.Focus();
        }
    }

    private static string Mask(string key)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;
        return key.Length <= 9 ? "sk-…" : $"{key[..5]}…{key[^4..]}";
    }

    private void SelectModel(string model)
    {
        int idx = _modelCombo.Items.IndexOf(model);
        if (idx < 0)
        {
            // A model not in our known list (custom): add and select it.
            idx = _modelCombo.Items.Add(model);
        }
        _modelCombo.SelectedIndex = idx;
        UpdateModelDesc();
    }

    private void UpdateModelDesc()
    {
        string id = _modelCombo.SelectedItem as string ?? string.Empty;
        Tr? desc = null;
        foreach ((string mId, Tr mDesc) in ModelChoices)
        {
            if (mId == id) { desc = mDesc; break; }
        }
        _modelDesc.Text = desc is null ? string.Empty : Localization.Get(desc.Value);
    }

    private void UpdateShortcutPreview()
    {
        var preview = new HotkeyCombo
        {
            Control = _ctrl.Checked,
            Alt = _alt.Checked,
            Shift = _shift.Checked,
            Win = _win.Checked,
            Key = SelectedKey(),
        };
        _shortcutPreview.Text = Localization.Get(Tr.ShortcutPreview, preview.ToString());
    }

    // ---- Key combo ---------------------------------------------------------

    private sealed class KeyItem
    {
        public Keys Key { get; init; }
        public string Label { get; init; } = string.Empty;
        public override string ToString() => Label;
    }

    private void PopulateKeyCombo()
    {
        for (char c = 'A'; c <= 'Z'; c++)
        {
            _keyCombo.Items.Add(new KeyItem { Key = (Keys)c, Label = c.ToString() });
        }
        for (int d = 0; d <= 9; d++)
        {
            _keyCombo.Items.Add(new KeyItem { Key = Keys.D0 + d, Label = d.ToString() });
        }
        for (int f = 1; f <= 12; f++)
        {
            _keyCombo.Items.Add(new KeyItem { Key = Keys.F1 + (f - 1), Label = "F" + f });
        }
    }

    private void SelectKey(Keys key)
    {
        for (int i = 0; i < _keyCombo.Items.Count; i++)
        {
            if (_keyCombo.Items[i] is KeyItem ki && ki.Key == key)
            {
                _keyCombo.SelectedIndex = i;
                return;
            }
        }
        // Fallback to "D" if the saved key isn't in our list.
        SelectKey(Keys.D);
    }

    private Keys SelectedKey() => _keyCombo.SelectedItem is KeyItem ki ? ki.Key : Keys.D;

    // ---- Save --------------------------------------------------------------

    private void Save_Click(object? sender, EventArgs e)
    {
        string model = _modelCombo.SelectedItem as string ?? string.Empty;
        if (string.IsNullOrWhiteSpace(model))
        {
            Warn(Localization.Get(Tr.ErrModelEmpty));
            return;
        }

        _hotkey.Control = _ctrl.Checked;
        _hotkey.Alt = _alt.Checked;
        _hotkey.Shift = _shift.Checked;
        _hotkey.Win = _win.Checked;
        _hotkey.Key = SelectedKey();

        if (!_hotkey.IsValid)
        {
            Warn(Localization.Get(Tr.ErrHotkeyModifier));
            return;
        }

        Settings.Model = model;
        Settings.AutoPaste = _autoInsert.Checked;
        Settings.StartWithWindows = _startup.Checked;
        Settings.Beep = _beep.Checked;
        // Settings.Hotkey is _hotkey (same reference), already updated.
    }

    private void Warn(string message)
    {
        MessageBox.Show(this, message, "deekta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        DialogResult = DialogResult.None;
    }
}
