using System.Drawing;
using System.Drawing.Drawing2D;
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
    private readonly Button _moreLangButton;
    private readonly ComboBox _modeCombo;
    private readonly ComboBox _targetLangCombo;
    private readonly Button _moreTargetLangButton;
    private readonly Label _targetLangLabel;
    private readonly Panel _targetLangPanel;
    private readonly Button _saveButton;
    private readonly Button _cancelButton;

    private bool _capturingKey;
    private Color _selectedBorderColor;
    private readonly string _currentLang;
    private string _selectedLangCode;
    private string _selectedTargetLangCode;

    public Keys HotKeyValue { get; private set; }
    public HotKeyModifierKeys SelectedModifiers { get; private set; }
    public string BorderColorHex => ColorTranslator.ToHtml(_selectedBorderColor);
    public bool MulticolorBorder => _multicolorCheck.Checked;
    public string SelectedLanguage => _selectedLangCode;
    public string SelectedMode => _modeCombo.SelectedIndex == 0 ? "transcription" : "translation";
    public string SelectedTargetLanguage => _selectedTargetLangCode;

    public SettingsForm(AppSettings currentSettings)
    {
        _currentLang = currentSettings.AppLanguage;
        _selectedLangCode = currentSettings.AppLanguage;
        _selectedTargetLangCode = currentSettings.TargetLanguage;
        HotKeyValue = currentSettings.HotKey;
        SelectedModifiers = currentSettings.Modifiers;
        _selectedBorderColor = ColorTranslator.FromHtml(currentSettings.BorderColor);

        _hotKeyTextBox = CreateHotKeyTextBox();
        _captureButton = CreateCaptureButton();
        _ctrlCheck = CreateModifierCheckBox("Ctrl", HotKeyModifierKeys.Control);
        _altCheck = CreateModifierCheckBox("Alt", HotKeyModifierKeys.Alt);
        _shiftCheck = CreateModifierCheckBox("Shift", HotKeyModifierKeys.Shift);
        _winCheck = CreateModifierCheckBox("Win", HotKeyModifierKeys.Win);
        _colorButton = CreateStyledButton(WidgetStrings.Get("settings_capture", _currentLang), 100, Color.FromArgb(60, 60, 65));
        _colorButton.Click += OnColorButtonClick;
        _colorPreview = CreateColorPreview();
        _colorHexLabel = CreateColorHexLabel();
        _multicolorCheck = CreateMulticolorCheckBox();
        _multicolorCheck.Checked = currentSettings.MulticolorBorder;

        _languageCombo = CreateLanguageCombo(currentSettings.AppLanguage);
        _moreLangButton = CreateSmallButton("...");
        _modeCombo = CreateModeCombo(currentSettings.Mode);
        _targetLangCombo = CreateTargetLangCombo(currentSettings.TargetLanguage);
        _moreTargetLangButton = CreateSmallButton("...");
        _targetLangLabel = CreateLabel(WidgetStrings.Get("settings_target_lang", _currentLang));

        _cancelButton = CreateStyledButton(WidgetStrings.Get("settings_cancel", _currentLang), 90, Color.FromArgb(60, 60, 65));
        _cancelButton.DialogResult = DialogResult.Cancel;
        _saveButton = CreateStyledButton(WidgetStrings.Get("settings_save", _currentLang), 90, Color.FromArgb(79, 110, 247));
        _saveButton.Click += OnSaveClick;
        _saveButton.DialogResult = DialogResult.OK;

        _targetLangPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 10),
            Visible = currentSettings.Mode == "translation"
        };
        _targetLangPanel.Controls.Add(_targetLangLabel);
        _targetLangPanel.Controls.Add(_targetLangCombo);
        _targetLangPanel.Controls.Add(_moreTargetLangButton);

        _modeCombo.SelectedIndexChanged += (_, _) =>
        {
            _targetLangPanel.Visible = _modeCombo.SelectedIndex == 1;
        };

        _moreLangButton.Click += (_, _) => ShowLanguagePicker(false);
        _moreTargetLangButton.Click += (_, _) => ShowLanguagePicker(true);

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
        Size = new Size(500, 640);
        BackColor = Color.FromArgb(30, 30, 30);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9);
        KeyPreview = true;
        AutoScroll = true;

        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24),
            ColumnCount = 1,
            RowCount = 18,
            BackColor = Color.Transparent,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        for (int i = 0; i < 18; i++)
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainPanel.RowStyles[17] = new RowStyle(SizeType.Percent, 100);

        var lblLangHeader = CreateSectionLabel(WidgetStrings.Get("settings_language", _currentLang));

        var langPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 10)
        };
        langPanel.Controls.Add(_languageCombo);
        langPanel.Controls.Add(_moreLangButton);

        var lblModeHeader = CreateSectionLabel(WidgetStrings.Get("settings_mode", _currentLang));

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
            ForeColor = Color.FromArgb(120, 120, 120),
            Margin = new Padding(25, 5, 0, 15)
        };

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new Padding(0, 12, 0, 0)
        };
        buttonPanel.Controls.Add(_cancelButton);
        buttonPanel.Controls.Add(_saveButton);

        mainPanel.Controls.Add(lblLangHeader, 0, 0);
        mainPanel.Controls.Add(langPanel, 0, 1);
        mainPanel.Controls.Add(lblModeHeader, 0, 2);
        mainPanel.Controls.Add(_modeCombo, 0, 3);
        mainPanel.Controls.Add(_targetLangPanel, 0, 4);
        mainPanel.Controls.Add(new Panel { Height = 8 }, 0, 5);
        mainPanel.Controls.Add(lblHotkey, 0, 6);
        mainPanel.Controls.Add(hotKeyPanel, 0, 7);
        mainPanel.Controls.Add(lblModifiers, 0, 8);
        mainPanel.Controls.Add(modifiersPanel, 0, 9);
        mainPanel.Controls.Add(new Panel { Height = 8 }, 0, 10);
        mainPanel.Controls.Add(lblAppearance, 0, 11);
        mainPanel.Controls.Add(lblBorderColor, 0, 12);
        mainPanel.Controls.Add(colorPanel, 0, 13);
        mainPanel.Controls.Add(_multicolorCheck, 0, 14);
        mainPanel.Controls.Add(lblMulticolor, 0, 15);
        mainPanel.Controls.Add(new Panel { Height = 20 }, 0, 16);
        mainPanel.Controls.Add(buttonPanel, 0, 17);

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
            ForeColor = Color.FromArgb(79, 110, 247),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Margin = new Padding(0, 8, 0, 6)
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
            Width = 280,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(45, 45, 48),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Standard,
            Margin = new Padding(0, 0, 6, 0)
        };

        int selectedIndex = 0;
        for (int i = 0; i < AppLanguages.CommonLanguages.Length; i++)
        {
            combo.Items.Add($"{AppLanguages.CommonLanguages[i].DisplayName} ({AppLanguages.CommonLanguages[i].Code})");
            if (AppLanguages.CommonLanguages[i].Code == currentLang)
                selectedIndex = i;
        }
        combo.Items.Add("─────────────────────");
        combo.Items.Add(WidgetStrings.Get("settings_all_languages", _currentLang));

        var commonIdx = AppLanguages.FindCommonIndex(currentLang);
        if (commonIdx >= 0)
        {
            combo.SelectedIndex = commonIdx;
        }
        else
        {
            combo.SelectedIndex = 0;
        }

        combo.SelectedIndexChanged += (_, _) =>
        {
            if (combo.SelectedIndex < AppLanguages.CommonLanguages.Length)
            {
                _selectedLangCode = AppLanguages.CommonLanguages[combo.SelectedIndex].Code;
            }
            else if (combo.SelectedIndex == AppLanguages.CommonLanguages.Length + 1)
            {
                ShowLanguagePicker(false);
            }
        };

        return combo;
    }

    private ComboBox CreateModeCombo(string currentMode)
    {
        var combo = new ComboBox
        {
            Width = 280,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(45, 45, 48),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Standard,
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
            Width = 260,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(45, 45, 48),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Standard,
            Margin = new Padding(0, 0, 6, 0)
        };

        for (int i = 0; i < AppLanguages.CommonLanguages.Length; i++)
        {
            combo.Items.Add($"{AppLanguages.CommonLanguages[i].DisplayName} ({AppLanguages.CommonLanguages[i].Code})");
        }
        combo.Items.Add("─────────────────────");
        combo.Items.Add(WidgetStrings.Get("settings_all_languages", _currentLang));

        var commonIdx = AppLanguages.FindCommonIndex(currentTarget);
        if (commonIdx >= 0)
        {
            combo.SelectedIndex = commonIdx;
        }
        else
        {
            combo.SelectedIndex = 0;
        }

        combo.SelectedIndexChanged += (_, _) =>
        {
            if (combo.SelectedIndex < AppLanguages.CommonLanguages.Length)
            {
                _selectedTargetLangCode = AppLanguages.CommonLanguages[combo.SelectedIndex].Code;
            }
            else if (combo.SelectedIndex == AppLanguages.CommonLanguages.Length + 1)
            {
                ShowLanguagePicker(true);
            }
        };

        return combo;
    }

    private void ShowLanguagePicker(bool isTarget)
    {
        using var pickerForm = new LanguagePickerForm(isTarget ? _selectedTargetLangCode : _selectedLangCode);
        if (pickerForm.ShowDialog(this) == DialogResult.OK)
        {
            if (isTarget)
            {
                _selectedTargetLangCode = pickerForm.SelectedCode;
                var idx = AppLanguages.FindCommonIndex(_selectedTargetLangCode);
                if (idx >= 0)
                {
                    _targetLangCombo.SelectedIndex = idx;
                }
                else
                {
                    _targetLangCombo.Items[_targetLangCombo.Items.Count - 1] =
                        $"{AppLanguages.GetDisplayName(_selectedTargetLangCode)} ({_selectedTargetLangCode})";
                    _targetLangCombo.SelectedIndex = _targetLangCombo.Items.Count - 1;
                }
            }
            else
            {
                _selectedLangCode = pickerForm.SelectedCode;
                var idx = AppLanguages.FindCommonIndex(_selectedLangCode);
                if (idx >= 0)
                {
                    _languageCombo.SelectedIndex = idx;
                }
                else
                {
                    _languageCombo.Items[_languageCombo.Items.Count - 1] =
                        $"{AppLanguages.GetDisplayName(_selectedLangCode)} ({_selectedLangCode})";
                    _languageCombo.SelectedIndex = _languageCombo.Items.Count - 1;
                }
            }
        }
    }

    private TextBox CreateHotKeyTextBox()
    {
        var textBox = new TextBox
        {
            ReadOnly = true,
            Width = 220,
            BackColor = Color.FromArgb(45, 45, 48),
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
        var button = CreateStyledButton(WidgetStrings.Get("settings_capture", _currentLang), 90, Color.FromArgb(79, 110, 247));
        button.Click += (_, _) => BeginCapture();
        return button;
    }

    private static Button CreateStyledButton(string text, int width, Color bgColor)
    {
        var button = new Button
        {
            Text = text,
            Size = new Size(width, 32),
            BackColor = bgColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    private static Button CreateSmallButton(string text)
    {
        var button = new Button
        {
            Text = text,
            Size = new Size(32, 28),
            BackColor = Color.FromArgb(60, 60, 65),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 0, 0)
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    private Label CreateColorPreview()
    {
        return new Label
        {
            Size = new Size(24, 24),
            BackColor = _selectedBorderColor,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(8, 2, 8, 0)
        };
    }

    private Label CreateColorHexLabel()
    {
        return new Label
        {
            Text = ColorTranslator.ToHtml(_selectedBorderColor),
            AutoSize = true,
            ForeColor = Color.FromArgb(120, 120, 120),
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
        checkBox.FlatAppearance.CheckedBackColor = Color.FromArgb(79, 110, 247);
        checkBox.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 85);
        return checkBox;
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

public sealed class LanguagePickerForm : Form
{
    public string SelectedCode { get; private set; }
    private readonly TextBox _searchBox;
    private readonly ListBox _listBox;

    public LanguagePickerForm(string currentCode)
    {
        SelectedCode = currentCode;

        Text = "Seleccionar idioma";
        Size = new Size(380, 460);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(30, 30, 30);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9);

        var lblSearch = new Label
        {
            Text = "Buscar idioma:",
            AutoSize = true,
            ForeColor = Color.FromArgb(120, 120, 120),
            Location = new Point(15, 12)
        };

        _searchBox = new TextBox
        {
            Location = new Point(15, 34),
            Width = 335,
            BackColor = Color.FromArgb(45, 45, 48),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10)
        };
        _searchBox.TextChanged += (_, _) => FilterLanguages();

        _listBox = new ListBox
        {
            Location = new Point(15, 66),
            Width = 335,
            Height = 290,
            BackColor = Color.FromArgb(40, 40, 44),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 9.5f)
        };
        _listBox.DoubleClick += (_, _) => SelectAndClose();

        var okButton = new Button
        {
            Text = "Seleccionar",
            Location = new Point(185, 366),
            Width = 90,
            Height = 32,
            BackColor = Color.FromArgb(79, 110, 247),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            DialogResult = DialogResult.OK
        };
        okButton.FlatAppearance.BorderSize = 0;
        okButton.Click += (_, _) => SelectAndClose();

        var cancelButton = new Button
        {
            Text = "Cancelar",
            Location = new Point(280, 366),
            Width = 70,
            Height = 32,
            BackColor = Color.FromArgb(60, 60, 65),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            DialogResult = DialogResult.Cancel
        };
        cancelButton.FlatAppearance.BorderSize = 0;

        Controls.AddRange(new Control[] { lblSearch, _searchBox, _listBox, okButton, cancelButton });
        AcceptButton = okButton;
        CancelButton = cancelButton;

        LoadLanguages();
        _searchBox.Focus();
    }

    private void LoadLanguages()
    {
        _listBox.Items.Clear();
        foreach (var lang in AppLanguages.AllLanguages)
        {
            _listBox.Items.Add($"{lang.DisplayName} ({lang.Code})");
        }

        var idx = AppLanguages.FindAllIndex(SelectedCode);
        if (idx >= 0) _listBox.SelectedIndex = idx;
    }

    private void FilterLanguages()
    {
        var query = _searchBox.Text.ToLowerInvariant().Trim();
        _listBox.Items.Clear();

        foreach (var lang in AppLanguages.AllLanguages)
        {
            if (string.IsNullOrEmpty(query) ||
                lang.DisplayName.ToLowerInvariant().Contains(query) ||
                lang.Code.ToLowerInvariant().Contains(query))
            {
                _listBox.Items.Add($"{lang.DisplayName} ({lang.Code})");
            }
        }

        if (_listBox.Items.Count > 0) _listBox.SelectedIndex = 0;
    }

    private void SelectAndClose()
    {
        if (_listBox.SelectedItem is string selected)
        {
            var start = selected.LastIndexOf('(') + 1;
            var end = selected.LastIndexOf(')');
            if (start > 0 && end > start)
            {
                SelectedCode = selected[start..end];
            }
        }
    }
}
