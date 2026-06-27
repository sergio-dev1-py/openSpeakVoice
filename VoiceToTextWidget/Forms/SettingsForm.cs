using System.Drawing;
using System.Windows.Forms;
using VoiceToTextWidget.Models;
using VoiceToTextWidget.Services;
using HotKeyModifierKeys = VoiceToTextWidget.Models.ModifierKeys;

namespace VoiceToTextWidget.Forms;

public sealed class SettingsForm : Form
{
    private readonly TextBox _hotKeyTextBox;
    private readonly Button _captureButton;
    private readonly CheckBox _ctrlCheck;
    private readonly CheckBox _altCheck;
    private readonly CheckBox _shiftCheck;
    private readonly CheckBox _winCheck;
    private readonly Button _colorButton;
    private readonly Label _colorPreview;
    private readonly Label _colorHexLabel;
    private readonly CheckBox _multicolorCheck;
    private readonly ComboBox _languageCombo;
    private readonly ComboBox _modeCombo;
    private readonly ComboBox _targetLangCombo;
    private readonly Label _targetLangLabel;
    private readonly Button _saveButton;
    private readonly Button _cancelButton;

    private bool _capturingKey;
    private Color _selectedBorderColor;
    private readonly string _currentLang;

    public Keys HotKeyValue { get; private set; }
    public HotKeyModifierKeys SelectedModifiers { get; private set; }
    public string BorderColorHex => ColorTranslator.ToHtml(_selectedBorderColor);
    public bool MulticolorBorder => _multicolorCheck.Checked;
    public string SelectedLanguage => AppLanguages.Languages[_languageCombo.SelectedIndex].Code;
    public string SelectedMode => _modeCombo.SelectedIndex == 0 ? "transcription" : "translation";
    public string SelectedTargetLanguage => AppLanguages.TargetLanguages[_targetLangCombo.SelectedIndex].Code;

    public SettingsForm(AppSettings currentSettings)
    {
        _currentLang = currentSettings.AppLanguage;
        HotKeyValue = currentSettings.HotKey;
        SelectedModifiers = currentSettings.Modifiers;
        _selectedBorderColor = ColorTranslator.FromHtml(currentSettings.BorderColor);

        _hotKeyTextBox = CreateHotKeyTextBox();
        _captureButton = CreateCaptureButton();
        _ctrlCheck = CreateModifierCheckBox("Ctrl", HotKeyModifierKeys.Control);
        _altCheck = CreateModifierCheckBox("Alt", HotKeyModifierKeys.Alt);
        _shiftCheck = CreateModifierCheckBox("Shift", HotKeyModifierKeys.Shift);
        _winCheck = CreateModifierCheckBox("Win", HotKeyModifierKeys.Win);
        _colorButton = CreateColorButton();
        _colorPreview = CreateColorPreview();
        _colorHexLabel = CreateColorHexLabel();
        _multicolorCheck = CreateMulticolorCheckBox();
        _multicolorCheck.Checked = currentSettings.MulticolorBorder;

        _languageCombo = CreateLanguageCombo(currentSettings.AppLanguage);
        _modeCombo = CreateModeCombo(currentSettings.Mode);
        _targetLangCombo = CreateTargetLangCombo(currentSettings.TargetLanguage);
        _targetLangLabel = CreateLabel(WidgetStrings.Get("settings_target_lang", _currentLang));

        _cancelButton = CreateCancelButton();
        _saveButton = CreateSaveButton();

        InitializeComponent();
        LoadCurrentSettings();
    }

    private void InitializeComponent()
    {
        Text = WidgetStrings.Get("settings_language", _currentLang);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(480, 620);
        BackColor = Color.FromArgb(45, 45, 48);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9);
        KeyPreview = true;
        AutoScroll = true;

        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            ColumnCount = 1,
            RowCount = 17,
            BackColor = Color.Transparent,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        for (int i = 0; i < 17; i++)
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainPanel.RowStyles[16] = new RowStyle(SizeType.Percent, 100);

        var lblLangHeader = CreateSectionLabel(WidgetStrings.Get("settings_language", _currentLang));
        var lblLang = CreateLabel(WidgetStrings.Get("settings_language", _currentLang) + ":");

        var lblModeHeader = CreateSectionLabel(WidgetStrings.Get("settings_mode", _currentLang));
        var lblMode = CreateLabel(WidgetStrings.Get("settings_mode", _currentLang) + ":");

        var lblHotkey = CreateLabel(WidgetStrings.Get("settings_hotkey", _currentLang));
        var lblModifiers = CreateLabel(WidgetStrings.Get("settings_modifiers", _currentLang));

        var hotKeyPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 10)
        };
        hotKeyPanel.Controls.Add(_hotKeyTextBox);
        hotKeyPanel.Controls.Add(_captureButton);

        var modifiersPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 15)
        };
        modifiersPanel.Controls.AddRange(new Control[] { _ctrlCheck, _altCheck, _shiftCheck, _winCheck });

        var lblAppearance = CreateSectionLabel(WidgetStrings.Get("settings_appearance", _currentLang));
        var lblBorderColor = CreateLabel(WidgetStrings.Get("settings_border_color", _currentLang));

        var colorPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 10)
        };
        colorPanel.Controls.Add(_colorButton);
        colorPanel.Controls.Add(_colorPreview);
        colorPanel.Controls.Add(_colorHexLabel);

        var lblMulticolor = new Label
        {
            Text = WidgetStrings.Get("settings_multicolor_desc", _currentLang),
            AutoSize = true,
            ForeColor = Color.FromArgb(160, 160, 160),
            Margin = new Padding(25, 5, 0, 15)
        };

        var targetLangPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 10),
            Visible = _modeCombo.SelectedIndex == 1
        };
        targetLangPanel.Controls.Add(_targetLangLabel);
        targetLangPanel.Controls.Add(_targetLangCombo);

        _modeCombo.SelectedIndexChanged += (_, _) =>
        {
            targetLangPanel.Visible = _modeCombo.SelectedIndex == 1;
        };

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new Padding(0, 10, 0, 0)
        };
        buttonPanel.Controls.Add(_cancelButton);
        buttonPanel.Controls.Add(_saveButton);

        mainPanel.Controls.Add(lblLangHeader, 0, 0);
        mainPanel.Controls.Add(_languageCombo, 0, 1);
        mainPanel.Controls.Add(lblModeHeader, 0, 2);
        mainPanel.Controls.Add(_modeCombo, 0, 3);
        mainPanel.Controls.Add(targetLangPanel, 0, 4);
        mainPanel.Controls.Add(lblHotkey, 0, 5);
        mainPanel.Controls.Add(hotKeyPanel, 0, 6);
        mainPanel.Controls.Add(lblModifiers, 0, 7);
        mainPanel.Controls.Add(modifiersPanel, 0, 8);
        mainPanel.Controls.Add(lblAppearance, 0, 9);
        mainPanel.Controls.Add(lblBorderColor, 0, 10);
        mainPanel.Controls.Add(colorPanel, 0, 11);
        mainPanel.Controls.Add(_multicolorCheck, 0, 12);
        mainPanel.Controls.Add(lblMulticolor, 0, 13);
        mainPanel.Controls.Add(new Panel { Height = 20 }, 0, 14);
        mainPanel.Controls.Add(buttonPanel, 0, 16);

        Controls.Add(mainPanel);
        AcceptButton = _saveButton;
        CancelButton = _cancelButton;

        KeyDown += OnFormKeyDown;
    }

    private static Label CreateSectionLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            ForeColor = Color.FromArgb(120, 180, 255),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Margin = new Padding(0, 10, 0, 5)
        };
    }

    private static Label CreateLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            ForeColor = Color.White,
            Margin = new Padding(0, 0, 0, 5)
        };
    }

    private ComboBox CreateLanguageCombo(string currentLang)
    {
        var combo = new ComboBox
        {
            Width = 200,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(60, 60, 65),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 0, 0, 10)
        };

        int selectedIndex = 0;
        for (int i = 0; i < AppLanguages.Languages.Length; i++)
        {
            combo.Items.Add($"{AppLanguages.Languages[i].DisplayName} ({AppLanguages.Languages[i].Code})");
            if (AppLanguages.Languages[i].Code == currentLang)
                selectedIndex = i;
        }

        combo.SelectedIndex = selectedIndex;
        return combo;
    }

    private ComboBox CreateModeCombo(string currentMode)
    {
        var combo = new ComboBox
        {
            Width = 200,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(60, 60, 65),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 0, 0, 10)
        };

        combo.Items.Add(WidgetStrings.Get("mode_transcription", _currentLang));
        combo.Items.Add(WidgetStrings.Get("mode_translation", _currentLang));
        combo.SelectedIndex = currentMode == "translation" ? 1 : 0;
        return combo;
    }

    private ComboBox CreateTargetLangCombo(string currentTarget)
    {
        var combo = new ComboBox
        {
            Width = 200,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(60, 60, 65),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(10, 0, 0, 10)
        };

        int selectedIndex = 0;
        for (int i = 0; i < AppLanguages.TargetLanguages.Length; i++)
        {
            combo.Items.Add($"{AppLanguages.TargetLanguages[i].DisplayName} ({AppLanguages.TargetLanguages[i].Code})");
            if (AppLanguages.TargetLanguages[i].Code == currentTarget)
                selectedIndex = i;
        }

        combo.SelectedIndex = selectedIndex;
        return combo;
    }

    private TextBox CreateHotKeyTextBox()
    {
        var textBox = new TextBox
        {
            ReadOnly = true,
            Width = 220,
            BackColor = Color.FromArgb(60, 60, 65),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9),
            Margin = new Padding(0, 0, 10, 0),
            TabStop = true
        };
        textBox.KeyDown += OnHotKeyTextBoxKeyDown;
        return textBox;
    }

    private Button CreateCaptureButton()
    {
        var button = new Button
        {
            Text = WidgetStrings.Get("settings_capture", _currentLang),
            Size = new Size(90, 28),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(0, 100, 190);
        button.Click += (_, _) => BeginCapture();
        return button;
    }

    private Button CreateColorButton()
    {
        var button = new Button
        {
            Text = "Seleccionar",
            Size = new Size(100, 28),
            BackColor = Color.FromArgb(60, 60, 65),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 0, 10, 0)
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 85);
        button.Click += OnColorButtonClick;
        return button;
    }

    private Label CreateColorPreview()
    {
        return new Label
        {
            Size = new Size(24, 24),
            BackColor = _selectedBorderColor,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 2, 8, 0)
        };
    }

    private Label CreateColorHexLabel()
    {
        return new Label
        {
            Text = ColorTranslator.ToHtml(_selectedBorderColor),
            AutoSize = true,
            ForeColor = Color.FromArgb(160, 160, 160),
            Font = new Font("Segoe UI", 9),
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private CheckBox CreateMulticolorCheckBox()
    {
        return new CheckBox
        {
            Text = WidgetStrings.Get("settings_multicolor", _currentLang),
            AutoSize = true,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9),
            Margin = new Padding(0, 5, 0, 0),
            Checked = false
        };
    }

    private CheckBox CreateModifierCheckBox(string text, HotKeyModifierKeys modifier)
    {
        var checkBox = new CheckBox
        {
            Text = text,
            AutoSize = true,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9),
            Margin = new Padding(0, 0, 15, 0),
            FlatStyle = FlatStyle.Flat,
            Tag = modifier
        };
        checkBox.CheckedChanged += (_, _) =>
        {
            if (!_capturingKey)
            {
                RefreshModifiersFromChecks();
                RefreshHotKeyDisplay();
            }
        };
        checkBox.FlatAppearance.CheckedBackColor = Color.FromArgb(0, 120, 215);
        checkBox.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 105);
        return checkBox;
    }

    private Button CreateCancelButton()
    {
        var button = new Button
        {
            Text = WidgetStrings.Get("settings_cancel", _currentLang),
            Size = new Size(90, 32),
            BackColor = Color.FromArgb(80, 80, 85),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            DialogResult = DialogResult.Cancel
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 105);
        return button;
    }

    private Button CreateSaveButton()
    {
        var button = new Button
        {
            Text = WidgetStrings.Get("settings_save", _currentLang),
            Size = new Size(90, 32),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            DialogResult = DialogResult.OK
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(0, 100, 190);
        button.Click += OnSaveClick;
        return button;
    }

    private void LoadCurrentSettings()
    {
        _ctrlCheck.Checked = (SelectedModifiers & HotKeyModifierKeys.Control) != 0;
        _altCheck.Checked = (SelectedModifiers & HotKeyModifierKeys.Alt) != 0;
        _shiftCheck.Checked = (SelectedModifiers & HotKeyModifierKeys.Shift) != 0;
        _winCheck.Checked = (SelectedModifiers & HotKeyModifierKeys.Win) != 0;

        RefreshHotKeyDisplay();
        RefreshColorDisplay();
    }

    private void OnColorButtonClick(object? sender, EventArgs e)
    {
        using var colorDialog = new ColorDialog
        {
            Color = _selectedBorderColor,
            FullOpen = true,
            AnyColor = true
        };

        if (colorDialog.ShowDialog(this) == DialogResult.OK)
        {
            _selectedBorderColor = colorDialog.Color;
            RefreshColorDisplay();
        }
    }

    private void RefreshColorDisplay()
    {
        _colorPreview.BackColor = _selectedBorderColor;
        _colorHexLabel.Text = ColorTranslator.ToHtml(_selectedBorderColor);
    }

    private void BeginCapture()
    {
        _capturingKey = true;
        _hotKeyTextBox.Text = WidgetStrings.Get("settings_capture_prompt", _currentLang);
        _hotKeyTextBox.Focus();
        _hotKeyTextBox.SelectAll();
    }

    private void OnFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (!_capturingKey) return;
        CapturePressedKey(e);
    }

    private void OnHotKeyTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (!_capturingKey) return;
        CapturePressedKey(e);
    }

    private void CapturePressedKey(KeyEventArgs e)
    {
        if (IsModifierOnlyKey(e.KeyCode)) return;

        if (e.KeyCode == Keys.Escape)
        {
            _capturingKey = false;
            RefreshHotKeyDisplay();
            e.SuppressKeyPress = true;
            return;
        }

        HotKeyValue = e.KeyCode;
        _ctrlCheck.Checked = e.Control;
        _altCheck.Checked = e.Alt;
        _shiftCheck.Checked = e.Shift;
        RefreshModifiersFromChecks();

        _capturingKey = false;
        RefreshHotKeyDisplay();
        e.SuppressKeyPress = true;
    }

    private void RefreshModifiersFromChecks()
    {
        SelectedModifiers = HotKeyModifierKeys.None;
        if (_ctrlCheck.Checked) SelectedModifiers |= HotKeyModifierKeys.Control;
        if (_altCheck.Checked) SelectedModifiers |= HotKeyModifierKeys.Alt;
        if (_shiftCheck.Checked) SelectedModifiers |= HotKeyModifierKeys.Shift;
        if (_winCheck.Checked) SelectedModifiers |= HotKeyModifierKeys.Win;
    }

    private void RefreshHotKeyDisplay()
    {
        _hotKeyTextBox.Text = FormatHotKey(HotKeyValue, SelectedModifiers);
    }

    private static string FormatHotKey(Keys key, HotKeyModifierKeys modifiers)
    {
        var parts = new List<string>();
        if ((modifiers & HotKeyModifierKeys.Control) != 0) parts.Add("Ctrl");
        if ((modifiers & HotKeyModifierKeys.Alt) != 0) parts.Add("Alt");
        if ((modifiers & HotKeyModifierKeys.Shift) != 0) parts.Add("Shift");
        if ((modifiers & HotKeyModifierKeys.Win) != 0) parts.Add("Win");
        parts.Add(key.ToString());
        return string.Join(" + ", parts);
    }

    private static bool IsModifierOnlyKey(Keys key)
    {
        return key is Keys.ControlKey
            or Keys.LControlKey
            or Keys.RControlKey
            or Keys.Menu
            or Keys.LMenu
            or Keys.RMenu
            or Keys.ShiftKey
            or Keys.LShiftKey
            or Keys.RShiftKey
            or Keys.LWin
            or Keys.RWin
            or Keys.None;
    }

    private void OnSaveClick(object? sender, EventArgs e)
    {
        if (IsModifierOnlyKey(HotKeyValue))
        {
            MessageBox.Show(this,
                WidgetStrings.Get("settings_invalid_key", _currentLang),
                WidgetStrings.Get("settings_language", _currentLang),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        RefreshModifiersFromChecks();
        RefreshHotKeyDisplay();
        DialogResult = DialogResult.OK;
        Close();
    }
}
