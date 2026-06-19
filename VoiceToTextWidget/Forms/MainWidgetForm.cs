using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using VoiceToTextWidget.Models;
using VoiceToTextWidget.Native;
using VoiceToTextWidget.Services;

namespace VoiceToTextWidget.Forms;

public sealed class MainWidgetForm : Form
{
    private readonly SettingsService _settingsService;
    private readonly AudioCaptureService _audioCapture;
    private readonly SpeechRecognitionService _speechRecognition;
    private readonly TextInjectionService _textInjection;
    
    private HotKeyService? _hotKeyService;
    
    private readonly Label _statusLabel;
    private readonly ContextMenuStrip _contextMenu;
    private readonly System.Windows.Forms.Timer _topMostTimer;
    private AppState _currentState = AppState.Idle;
    private bool _isDragging;
    private Point _dragStart;

    public MainWidgetForm(
        SettingsService settingsService,
        AudioCaptureService audioCapture,
        SpeechRecognitionService speechRecognition,
        TextInjectionService textInjection)
    {
        _settingsService = settingsService;
        _audioCapture = audioCapture;
        _speechRecognition = speechRecognition;
        _textInjection = textInjection;

        _statusLabel = InitializeComponent();
        _contextMenu = SetupContextMenu();
        ContextMenuStrip = _contextMenu;
        _audioCapture.AudioChunkCaptured += OnAudioChunkCaptured;

        _topMostTimer = new System.Windows.Forms.Timer { Interval = 2000 };
        _topMostTimer.Tick += (_, _) => EnforceTopMost();
        _topMostTimer.Start();

        Deactivate += OnDeactivate;

        RestorePosition();
        UpdateUI();
    }

    public void SetHotKeyService(HotKeyService hotKeyService)
    {
        _hotKeyService = hotKeyService;
        _hotKeyService.HotKeyPressed += OnHotKeyPressed;
    }

    private void EnforceTopMost()
    {
        if (IsHandleCreated && Visible)
        {
            WinApi.SetWindowPos(
                Handle,
                WinApi.HWND_TOPMOST,
                0, 0, 0, 0,
                WinApi.SWP_NOMOVE | WinApi.SWP_NOSIZE | WinApi.SWP_NOACTIVATE | WinApi.SWP_SHOWWINDOW);
        }
    }

    private void OnDeactivate(object? sender, EventArgs e)
    {
        if (IsHandleCreated && Visible)
        {
            WinApi.SetWindowPos(
                Handle,
                WinApi.HWND_TOPMOST,
                0, 0, 0, 0,
                WinApi.SWP_NOMOVE | WinApi.SWP_NOSIZE | WinApi.SWP_NOACTIVATE | WinApi.SWP_SHOWWINDOW);
        }
    }

    private Label InitializeComponent()
    {
        Text = "VoiceToText";
        FormBorderStyle = FormBorderStyle.None;
        TopMost = true;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Size = new Size(220, 44);
        BackColor = Color.FromArgb(40, 40, 40);
        DoubleBuffered = true;

        var statusLabel = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Cursor = Cursors.SizeAll
        };
        statusLabel.MouseDown += OnMouseDown;
        statusLabel.MouseMove += OnMouseMove;
        statusLabel.MouseUp += OnMouseUp;
        Controls.Add(statusLabel);

        MouseDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseUp += OnMouseUp;
        
        return statusLabel;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x00000008;
            return cp;
        }
    }

    private ContextMenuStrip SetupContextMenu()
    {
        var contextMenu = new ContextMenuStrip
        {
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.White
        };

        var settingsItem = new ToolStripMenuItem("Configurar tecla...")
        {
            ForeColor = Color.White,
            BackColor = Color.FromArgb(50, 50, 50)
        };
        settingsItem.Click += (_, _) => ShowSettingsDialog();

        var apiKeyItem = new ToolStripMenuItem("Configurar API key...")
        {
            ForeColor = Color.White,
            BackColor = Color.FromArgb(50, 50, 50)
        };
        apiKeyItem.Click += (_, _) => ShowApiKeyDialog();

        var separator = new ToolStripSeparator { BackColor = Color.FromArgb(60, 60, 60) };

        var exitItem = new ToolStripMenuItem("Salir")
        {
            ForeColor = Color.White,
            BackColor = Color.FromArgb(50, 50, 50)
        };
        exitItem.Click += (_, _) => Application.Exit();

        contextMenu.Items.AddRange(new ToolStripItem[] { settingsItem, apiKeyItem, separator, exitItem });
        return contextMenu;
    }

    private void RestorePosition()
    {
        var settings = _settingsService.Settings;
        Location = new Point(settings.WidgetPosX, settings.WidgetPosY);
        
        var screen = Screen.FromPoint(Location);
        if (!screen.WorkingArea.Contains(Bounds))
        {
            Location = new Point(screen.WorkingArea.Right - Width - 10, screen.WorkingArea.Bottom - Height - 10);
        }
    }

    private void SavePosition()
    {
        _settingsService.UpdateWidgetPosition(Location.X, Location.Y);
    }

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isDragging = true;
            _dragStart = e.Location;
        }
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            var delta = Point.Subtract(e.Location, new Size(_dragStart));
            Location = Point.Add(Location, new Size(delta));
        }
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && _isDragging)
        {
            _isDragging = false;
            SavePosition();
        }
    }

    private void OnHotKeyPressed()
    {
        if (InvokeRequired)
        {
            Invoke(new Action(OnHotKeyPressed));
            return;
        }

        ToggleRecording();
    }

    private async void ToggleRecording()
    {
        if (_currentState == AppState.Idle)
        {
            if (string.IsNullOrWhiteSpace(_settingsService.Settings.ApiKey))
            {
                ShowError("API key not configured. Right-click the widget and select 'Configure API key' to set your Groq API key.");
                return;
            }

            try
            {
                _audioCapture.StartRecording();
                _currentState = AppState.Listening;
                UpdateUI();
            }
            catch (Exception ex)
            {
                _currentState = AppState.Idle;
                UpdateUI();
                ShowError($"No se pudo iniciar la grabacion: {ex.Message}", ex);
            }
        }
        else if (_currentState == AppState.Listening)
        {
            _currentState = AppState.Transcribing;
            UpdateUI();

            try
            {
                var audioData = await _audioCapture.StopRecordingAsync();
                if (audioData.Length == 0)
                {
                    ShowError("No se capturo audio del microfono. Revisa que el microfono correcto este activo.");
                    return;
                }

                var text = await _speechRecognition.FinishSessionAsync(audioData);
                if (string.IsNullOrWhiteSpace(text))
                {
                    ShowError("No se reconocio texto. Prueba hablando mas claro o mas fuerte.");
                    return;
                }

                _textInjection.InjectText(text);
            }
            catch (Exception ex)
            {
                ShowError($"Error durante la transcripcion o el pegado: {ex.Message}", ex);
            }
            finally
            {
                _currentState = AppState.Idle;
                UpdateUI();
            }
        }
    }

    private void ShowError(string message, Exception? exception = null)
    {
        Debug.WriteLine(message);
        if (exception != null)
        {
            Debug.WriteLine(exception);
        }

        MessageBox.Show(this, message, "VoiceToText", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void OnAudioChunkCaptured(byte[] buffer, int bytesRecorded)
    {
    }

    private void UpdateUI()
    {
        _statusLabel.Text = _currentState switch
        {
            AppState.Idle => "Inactivo",
            AppState.Listening => "Escuchando...",
            AppState.Transcribing => "Transcribiendo...",
            _ => "Inactivo"
        };

        _statusLabel.BackColor = _currentState switch
        {
            AppState.Idle => Color.FromArgb(40, 40, 40),
            AppState.Listening => Color.FromArgb(180, 40, 40),
            AppState.Transcribing => Color.FromArgb(180, 140, 40),
            _ => Color.FromArgb(40, 40, 40)
        };
        BackColor = _statusLabel.BackColor;
    }

    private void ShowSettingsDialog()
    {
        using var settingsForm = new SettingsForm(_settingsService.Settings);
        if (settingsForm.ShowDialog(this) == DialogResult.OK)
        {
            _settingsService.UpdateHotKey(settingsForm.HotKeyValue, settingsForm.SelectedModifiers);
            _hotKeyService?.Reregister();

            if (_hotKeyService != null && !_hotKeyService.IsRegistered)
            {
                ShowError("No se pudo registrar el nuevo atajo de teclado.");
            }
        }
    }

    private void ShowApiKeyDialog()
    {
        var currentKey = _settingsService.Settings.ApiKey;
        var maskedKey = string.IsNullOrEmpty(currentKey) ? "(no configurada)" : new string('*', Math.Min(currentKey.Length, 8));

        using var inputForm = new Form
        {
            Text = "Groq API Key",
            Size = new Size(400, 180),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Color.FromArgb(50, 50, 50)
        };

        var label = new Label
        {
            Text = $"API Key actual: {maskedKey}\n\nObtén tu API key gratis en:\nhttps://console.groq.com",
            AutoSize = true,
            Location = new Point(15, 15),
            ForeColor = Color.White,
            BackColor = Color.Transparent
        };

        var textBox = new TextBox
        {
            Location = new Point(15, 90),
            Width = 350,
            UseSystemPasswordChar = true,
            BackColor = Color.FromArgb(40, 40, 40),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        var okButton = new Button
        {
            Text = "Guardar",
            Location = new Point(200, 120),
            Width = 80,
            BackColor = Color.FromArgb(70, 130, 180),
            ForeColor = Color.White,
            DialogResult = DialogResult.OK
        };

        var cancelButton = new Button
        {
            Text = "Cancelar",
            Location = new Point(285, 120),
            Width = 80,
            BackColor = Color.FromArgb(80, 80, 80),
            ForeColor = Color.White,
            DialogResult = DialogResult.Cancel
        };

        inputForm.Controls.AddRange(new Control[] { label, textBox, okButton, cancelButton });
        inputForm.AcceptButton = okButton;
        inputForm.CancelButton = cancelButton;

        if (inputForm.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(textBox.Text))
        {
            _settingsService.UpdateApiKey(textBox.Text.Trim());
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _topMostTimer.Stop();
        _topMostTimer.Dispose();
        _audioCapture.AudioChunkCaptured -= OnAudioChunkCaptured;
        Deactivate -= OnDeactivate;
        if (_hotKeyService != null)
        {
            _hotKeyService.HotKeyPressed -= OnHotKeyPressed;
        }
        SavePosition();
        base.OnFormClosing(e);
    }
}
