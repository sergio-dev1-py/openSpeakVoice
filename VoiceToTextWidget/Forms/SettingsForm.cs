using System.Drawing;
using System.Windows.Forms;
using VoiceToTextWidget.Models;
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
    private readonly Button _saveButton;
    private readonly Button _cancelButton;

    private bool _capturingKey;

    public Keys HotKeyValue { get; private set; }
    public HotKeyModifierKeys SelectedModifiers { get; private set; }

    public SettingsForm(AppSettings currentSettings)
    {
        HotKeyValue = currentSettings.HotKey;
        SelectedModifiers = currentSettings.Modifiers;

        _hotKeyTextBox = CreateHotKeyTextBox();
        _captureButton = CreateCaptureButton();
        _ctrlCheck = CreateModifierCheckBox("Ctrl", HotKeyModifierKeys.Control);
        _altCheck = CreateModifierCheckBox("Alt", HotKeyModifierKeys.Alt);
        _shiftCheck = CreateModifierCheckBox("Shift", HotKeyModifierKeys.Shift);
        _winCheck = CreateModifierCheckBox("Win", HotKeyModifierKeys.Win);
        _cancelButton = CreateCancelButton();
        _saveButton = CreateSaveButton();

        InitializeComponent();
        LoadCurrentSettings();
    }

    private void InitializeComponent()
    {
        Text = "Configuracion";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(480, 400);
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
            RowCount = 5,
            BackColor = Color.Transparent
        };
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var lblKey = new Label
        {
            Text = "Atajo principal:",
            AutoSize = true,
            ForeColor = Color.White,
            Margin = new Padding(0, 0, 0, 5)
        };

        var lblModifiers = new Label
        {
            Text = "Modificadores opcionales:",
            AutoSize = true,
            ForeColor = Color.White,
            Margin = new Padding(0, 15, 0, 5)
        };

        var hotKeyPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 10)
        };

        var modifiersPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 15)
        };

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new Padding(0, 10, 0, 0)
        };

        hotKeyPanel.Controls.Add(_hotKeyTextBox);
        hotKeyPanel.Controls.Add(_captureButton);

        modifiersPanel.Controls.AddRange(new Control[] { _ctrlCheck, _altCheck, _shiftCheck, _winCheck });

        buttonPanel.Controls.Add(_cancelButton);
        buttonPanel.Controls.Add(_saveButton);

        mainPanel.Controls.Add(lblKey, 0, 0);
        mainPanel.Controls.Add(hotKeyPanel, 0, 1);
        mainPanel.Controls.Add(lblModifiers, 0, 2);
        mainPanel.Controls.Add(modifiersPanel, 0, 3);
        mainPanel.Controls.Add(buttonPanel, 0, 4);

        Controls.Add(mainPanel);
        AcceptButton = _saveButton;
        CancelButton = _cancelButton;

        KeyDown += OnFormKeyDown;
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
            Text = "Capturar",
            Size = new Size(90, 28),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(0, 100, 190);
        button.Click += (_, _) => BeginCapture();
        return button;
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
            Text = "Cancelar",
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
            Text = "Guardar",
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
    }

    private void BeginCapture()
    {
        _capturingKey = true;
        _hotKeyTextBox.Text = "Presiona una tecla...";
        _hotKeyTextBox.Focus();
        _hotKeyTextBox.SelectAll();
    }

    private void OnFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (!_capturingKey)
        {
            return;
        }

        CapturePressedKey(e);
    }

    private void OnHotKeyTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (!_capturingKey)
        {
            return;
        }

        CapturePressedKey(e);
    }

    private void CapturePressedKey(KeyEventArgs e)
    {
        if (IsModifierOnlyKey(e.KeyCode))
        {
            return;
        }

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
            MessageBox.Show(this, "Selecciona una tecla principal valida antes de guardar.", "Configuracion",
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
