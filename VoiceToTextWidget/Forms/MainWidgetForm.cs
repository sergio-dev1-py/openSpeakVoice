using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using VoiceToTextWidget.Models;
using VoiceToTextWidget.Native;
using VoiceToTextWidget.Services;

namespace VoiceToTextWidget.Forms;

public sealed class MainWidgetForm : Form
{
    private readonly SettingsService _settingsService;
    private readonly AudioCaptureService _audioCapture;
    private readonly GroqApiSpeechService _groqService;
    private readonly WhisperLocalSpeechService _localService;
    private ISpeechRecognitionService _activeSpeechService;
    private readonly TextInjectionService _textInjection;
    private readonly ApiKeyManager _apiKeyManager;

    private HotKeyService? _hotKeyService;

    private readonly ContextMenuStrip _contextMenu;
    private readonly System.Windows.Forms.Timer _topMostTimer;
    private readonly System.Windows.Forms.Timer _animationTimer;
    private AppState _currentState = AppState.Idle;
    private bool _isDragging;
    private Point _dragStart;
    private NotifyIcon? _trayIcon;
    private bool _isHidden;
    private ToolStripMenuItem? _hideMenuItem;
    private ToolStripMenuItem? _exitMenuItem;

    private int _pulseAlpha = 180;
    private int _pulseDirection = 1;
    private int _animFrame;
    private int _animTick;
    private float _colorHue = 0f;

    private const int WidgetWidth = 175;
    private const int WidgetHeight = 38;
    private const int CornerRadius = 18;

    private static readonly Color ColorIdle = Color.FromArgb(45, 45, 48);
    private static readonly Color ColorListening = Color.FromArgb(200, 50, 50);
    private static readonly Color ColorTranscribing = Color.FromArgb(200, 140, 40);

    public MainWidgetForm(
        SettingsService settingsService,
        AudioCaptureService audioCapture,
        GroqApiSpeechService groqService,
        WhisperLocalSpeechService localService,
        TextInjectionService textInjection,
        ApiKeyManager apiKeyManager)
    {
        _settingsService = settingsService;
        _audioCapture = audioCapture;
        _groqService = groqService;
        _localService = localService;
        _textInjection = textInjection;
        _apiKeyManager = apiKeyManager;

        _activeSpeechService = _settingsService.Settings.ActiveProvider == SttProvider.LocalWhisper
            ? _localService
            : _groqService;

        _contextMenu = SetupContextMenu();
        ContextMenuStrip = _contextMenu;
        _audioCapture.AudioChunkCaptured += OnAudioChunkCaptured;

        InitializeComponent();
        InitTrayIcon();

        _topMostTimer = new System.Windows.Forms.Timer { Interval = 2000 };
        _topMostTimer.Tick += (_, _) => EnforceTopMost();
        _topMostTimer.Start();

        _animationTimer = new System.Windows.Forms.Timer { Interval = 50 };
        _animationTimer.Tick += OnAnimationTick;
        _animationTimer.Start();

        Deactivate += OnDeactivate;

        RestorePosition();
        UpdateUI();
    }

    private void InitializeComponent()
    {
        Text = "VoiceToText";
        FormBorderStyle = FormBorderStyle.None;
        TopMost = true;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Size = new Size(WidgetWidth, WidgetHeight);
        DoubleBuffered = true;
        BackColor = ColorIdle;
    }

    private void InitTrayIcon()
    {
        _trayIcon = new NotifyIcon
        {
            Icon = CreateTrayIcon(),
            Visible = false,
            Text = "VoiceToText"
        };

        var trayMenu = new ContextMenuStrip
        {
            BackColor = Color.FromArgb(30, 30, 30),
            Renderer = new DarkMenuRenderer()
        };

        var showItem = new ToolStripMenuItem("Mostrar")
        {
            ForeColor = Color.White,
            BackColor = Color.FromArgb(30, 30, 30)
        };
        showItem.Click += (_, _) => ShowWidget();

        var exitItem = new ToolStripMenuItem("Salir")
        {
            ForeColor = Color.White,
            BackColor = Color.FromArgb(30, 30, 30)
        };
        exitItem.Click += (_, _) => Application.Exit();

        trayMenu.Items.AddRange(new ToolStripItem[] { showItem, exitItem });

        _trayIcon.ContextMenuStrip = trayMenu;
        _trayIcon.DoubleClick += (_, _) => ShowWidget();
    }

    private static Icon CreateTrayIcon()
    {
        using var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        using var pen = new Pen(Color.White, 1.5f);
        using var brush = new SolidBrush(Color.White);

        g.FillRoundedRect(brush, 6, 1, 4, 8, 2);

        using var arcPath = new GraphicsPath();
        arcPath.AddArc(4, 0, 8, 6, 180, 180);
        arcPath.CloseFigure();
        g.DrawPath(pen, arcPath);

        g.DrawLine(pen, 8, 9, 8, 12);
        g.DrawLine(pen, 5, 12, 11, 12);

        IntPtr hIcon = bmp.GetHicon();
        return Icon.FromHandle(hIcon);
    }

    private void HideWidget()
    {
        _isHidden = true;
        Hide();
        _trayIcon!.Visible = true;
    }

    private void ShowWidget()
    {
        _isHidden = false;
        _trayIcon!.Visible = false;
        Show();
        BringToFront();
        EnforceTopMost();
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

    public void SetHotKeyService(HotKeyService hotKeyService)
    {
        _hotKeyService = hotKeyService;
        _hotKeyService.HotKeyPressed += OnHotKeyPressed;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        var bgColor = GetAnimatedBackground();
        var borderColor = GetBorderColor();
        var bw = 5;

        var outerRect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var outerPath = GetRoundedRectPath(outerRect, CornerRadius);
        using var borderBrush = new SolidBrush(borderColor);
        g.FillPath(borderBrush, outerPath);

        var innerRect = new Rectangle(bw, bw, Width - 1 - bw * 2, Height - 1 - bw * 2);
        using var innerPath = GetRoundedRectPath(innerRect, CornerRadius - bw);
        using var bgBrush = new SolidBrush(bgColor);
        g.FillPath(bgBrush, innerPath);

        Region = new Region(outerPath);

        DrawMicIcon(g, innerRect);
        DrawStatusText(g, innerRect);
    }

    private Color GetAnimatedBackground()
    {
        return ColorIdle;
    }

    private Color GetBorderColor()
    {
        var userColor = Color.White;
        try { userColor = ColorTranslator.FromHtml(_settingsService.Settings.BorderColor); }
        catch { }

        if (_settingsService.Settings.MulticolorBorder)
        {
            return HslToRgb(_colorHue, 1.0f, 0.55f);
        }

        return userColor;
    }

    private static Color HslToRgb(float h, float s, float l)
    {
        h = h % 360f;
        float c = (1f - Math.Abs(2f * l - 1f)) * s;
        float x = c * (1f - Math.Abs(h / 60f % 2f - 1f));
        float m = l - c / 2f;

        float r, g, b;
        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        return Color.FromArgb(
            Math.Clamp((int)((r + m) * 255), 0, 255),
            Math.Clamp((int)((g + m) * 255), 0, 255),
            Math.Clamp((int)((b + m) * 255), 0, 255));
    }

    private void DrawMicIcon(Graphics g, Rectangle bounds)
    {
        Color micColor = _currentState != AppState.Idle
            ? GetBorderColor()
            : Color.FromArgb(140, 140, 140);

        int cx = bounds.Left + 22;
        int cy = bounds.Top + bounds.Height / 2;

        using var pen = new Pen(micColor, 2f);
        using var brush = new SolidBrush(micColor);

        g.FillRoundedRect(brush, cx - 5, cy - 10, 10, 15, 5);

        using var arcPath = new GraphicsPath();
        arcPath.AddArc(cx - 9, cy - 14, 18, 14, 180, 180);
        arcPath.CloseFigure();
        g.DrawPath(pen, arcPath);

        g.DrawLine(pen, cx, cy + 1, cx, cy + 7);
        g.DrawLine(pen, cx - 5, cy + 7, cx + 5, cy + 7);
    }

    private void DrawStatusText(Graphics g, Rectangle bounds)
    {
        string text;
        Color textColor;

        switch (_currentState)
        {
            case AppState.Listening:
                text = "Escuchando";
                textColor = Color.FromArgb(255, 255, 255);
                break;
            case AppState.Transcribing:
                var dots = new string('.', (_animFrame % 3) + 1);
                text = "Transcribiendo" + dots;
                textColor = Color.FromArgb(255, 255, 255);
                break;
            default:
                text = "Inactivo";
                textColor = Color.FromArgb(140, 140, 140);
                break;
        }

        using var font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        using var brush = new SolidBrush(textColor);
        var textSize = g.MeasureString(text, font);
        var textX = bounds.Left + 40;
        var textY = (bounds.Top + (bounds.Height - textSize.Height) / 2);
        g.DrawString(text, font, brush, textX, textY);
    }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        if (_settingsService.Settings.MulticolorBorder)
        {
            _colorHue += 1.5f;
            if (_colorHue >= 360f) _colorHue -= 360f;
            Invalidate();
        }

        if (_currentState == AppState.Listening)
        {
            _pulseAlpha += 8 * _pulseDirection;
            if (_pulseAlpha >= 255) { _pulseAlpha = 255; _pulseDirection = -1; }
            if (_pulseAlpha <= 140) { _pulseAlpha = 140; _pulseDirection = 1; }
            Invalidate();
        }
        else if (_currentState == AppState.Transcribing)
        {
            _animTick++;
            if (_animTick >= 8)
            {
                _animTick = 0;
                _animFrame++;
                Invalidate();
            }
        }
    }

    private ContextMenuStrip SetupContextMenu()
    {
        var contextMenu = new ContextMenuStrip
        {
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.White,
            Renderer = new DarkMenuRenderer()
        };

        var settingsItem = new ToolStripMenuItem("Configuracion...")
        {
            ForeColor = Color.White,
            BackColor = Color.FromArgb(30, 30, 30)
        };
        settingsItem.Click += (_, _) => ShowSettingsDialog();

        var apiKeyItem = new ToolStripMenuItem("Configurar API Keys...")
        {
            ForeColor = Color.White,
            BackColor = Color.FromArgb(30, 30, 30)
        };
        apiKeyItem.Click += (_, _) => ShowApiKeyDialog();

        var providerMenu = new ToolStripMenuItem("Proveedor de voz")
        {
            Name = "providerMenu",
            ForeColor = Color.White,
            BackColor = Color.FromArgb(30, 30, 30)
        };

        var groqItem = new ToolStripMenuItem("Groq API (Nube)")
        {
            ForeColor = Color.White,
            BackColor = Color.FromArgb(30, 30, 30),
            Checked = _settingsService.Settings.ActiveProvider == SttProvider.GroqApi
        };
        groqItem.Click += (_, _) => SwitchProvider(SttProvider.GroqApi);

        var localItem = new ToolStripMenuItem("Whisper Local (GPU/CPU)")
        {
            ForeColor = Color.White,
            BackColor = Color.FromArgb(30, 30, 30),
            Checked = _settingsService.Settings.ActiveProvider == SttProvider.LocalWhisper
        };
        localItem.Click += (_, _) => SwitchProvider(SttProvider.LocalWhisper);

        var modelConfigItem = new ToolStripMenuItem("Configurar modelo local...")
        {
            ForeColor = Color.White,
            BackColor = Color.FromArgb(30, 30, 30)
        };
        modelConfigItem.Click += (_, _) => ShowLocalModelDialog();

        providerMenu.DropDownItems.AddRange(new ToolStripItem[] { groqItem, localItem, modelConfigItem });

        _hideMenuItem = new ToolStripMenuItem("Ocultar")
        {
            ForeColor = Color.White,
            BackColor = Color.FromArgb(30, 30, 30),
            Enabled = false
        };
        _hideMenuItem.Click += (_, _) => HideWidget();

        _exitMenuItem = new ToolStripMenuItem("Salir")
        {
            ForeColor = Color.White,
            BackColor = Color.FromArgb(30, 30, 30)
        };
        _exitMenuItem.Click += (_, _) => Application.Exit();

        contextMenu.Items.AddRange(new ToolStripItem[] { settingsItem, apiKeyItem, providerMenu, _hideMenuItem, _exitMenuItem });
        return contextMenu;
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

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isDragging = true;
            _dragStart = e.Location;
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_isDragging)
        {
            var delta = Point.Subtract(e.Location, new Size(_dragStart));
            Location = Point.Add(Location, new Size(delta));
        }
        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && _isDragging)
        {
            _isDragging = false;
            SavePosition();
        }
        base.OnMouseUp(e);
    }

    private void OnHotKeyPressed()
    {
        if (InvokeRequired)
        {
            Invoke(new Action(OnHotKeyPressed));
            return;
        }

        if (_isHidden)
        {
            ShowWidget();
            return;
        }

        ToggleRecording();
    }

    private async void ToggleRecording()
    {
        if (_currentState == AppState.Idle)
        {
            if (_settingsService.Settings.ActiveProvider == SttProvider.GroqApi
                && !_settingsService.Settings.HasAnyKey)
            {
                ShowError("No hay API keys configuradas. Click derecho -> 'Configurar API Keys...'");
                return;
            }

            try
            {
                _audioCapture.StartRecording();
                _currentState = AppState.Listening;
                _pulseAlpha = 180;
                _pulseDirection = 1;
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
            _animFrame = 0;
            _animTick = 0;
            UpdateUI();

            try
            {
                var audioData = await _audioCapture.StopRecordingAsync();
                if (audioData.Length == 0)
                {
                    ShowError("No se capturo audio del microfono. Revisa que el microfono correcto este activo.");
                    return;
                }

                var text = await _activeSpeechService.FinishSessionAsync(audioData);
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
        if (_hideMenuItem != null)
            _hideMenuItem.Enabled = _currentState == AppState.Idle;
        if (_exitMenuItem != null)
            _exitMenuItem.Enabled = _currentState == AppState.Idle;
        Invalidate();
    }

    private void ShowSettingsDialog()
    {
        using var settingsForm = new SettingsForm(_settingsService.Settings);
        if (settingsForm.ShowDialog(this) == DialogResult.OK)
        {
            _settingsService.UpdateHotKey(settingsForm.HotKeyValue, settingsForm.SelectedModifiers);
            _settingsService.UpdateAppearance(settingsForm.BorderColorHex, settingsForm.MulticolorBorder);
            Invalidate();
            _hotKeyService?.Reregister();

            if (_hotKeyService != null && !_hotKeyService.IsRegistered)
            {
                ShowError("No se pudo registrar el nuevo atajo de teclado.");
            }
        }
    }

    private void ShowApiKeyDialog()
    {
        var s = _settingsService.Settings;
        var (used1, total1, used2, total2) = _apiKeyManager.GetUsageStatus();

        var masked1 = string.IsNullOrEmpty(s.ApiKey1) ? "(no configurada)" : $"****{s.ApiKey1[^4..]}";
        var masked2 = string.IsNullOrEmpty(s.ApiKey2) ? "(no configurada)" : $"****{s.ApiKey2[^4..]}";

        using var inputForm = new Form
        {
            Text = "Groq API Keys (Dual Mode)",
            Size = new Size(460, 340),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Color.FromArgb(30, 30, 30)
        };

        var lblTitle = new Label
        {
            Text = "Obtén keys gratis en: https://console.groq.com",
            AutoSize = true,
            Location = new Point(15, 10),
            ForeColor = Color.FromArgb(120, 180, 255),
            BackColor = Color.Transparent
        };

        var lblKey1Status = new Label
        {
            Text = $"Key 1: {masked1}  |  Usados: {used1}/{total1}",
            AutoSize = true,
            Location = new Point(15, 38),
            ForeColor = Color.White,
            BackColor = Color.Transparent
        };

        var txtKey1 = new TextBox
        {
            Location = new Point(15, 60),
            Width = 415,
            UseSystemPasswordChar = true,
            BackColor = Color.FromArgb(45, 45, 48),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Text = s.ApiKey1
        };

        var lblKey2Status = new Label
        {
            Text = $"Key 2: {masked2}  |  Usados: {used2}/{total2}",
            AutoSize = true,
            Location = new Point(15, 90),
            ForeColor = Color.White,
            BackColor = Color.Transparent
        };

        var txtKey2 = new TextBox
        {
            Location = new Point(15, 112),
            Width = 415,
            UseSystemPasswordChar = true,
            BackColor = Color.FromArgb(45, 45, 48),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Text = s.ApiKey2
        };

        var lblInfo = new Label
        {
            Text = "Las keys se guardan automáticamente.\nRotación automática al llegar a 19 req/min por key.",
            AutoSize = true,
            Location = new Point(15, 145),
            ForeColor = Color.FromArgb(160, 160, 160),
            BackColor = Color.Transparent
        };

        var okButton = new Button
        {
            Text = "Guardar",
            Location = new Point(250, 210),
            Width = 90,
            Height = 40,
            BackColor = Color.FromArgb(70, 130, 180),
            ForeColor = Color.White,
            DialogResult = DialogResult.OK
        };

        var cancelButton = new Button
        {
            Text = "Cancelar",
            Location = new Point(345, 210),
            Width = 90,
            Height = 40,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White,
            DialogResult = DialogResult.Cancel
        };

        inputForm.Controls.AddRange(new Control[]
        {
            lblTitle, lblKey1Status, txtKey1,
            lblKey2Status, txtKey2, lblInfo,
            okButton, cancelButton
        });
        inputForm.AcceptButton = okButton;
        inputForm.CancelButton = cancelButton;

        if (inputForm.ShowDialog(this) == DialogResult.OK)
        {
            _settingsService.UpdateApiKey1(txtKey1.Text.Trim());
            _settingsService.UpdateApiKey2(txtKey2.Text.Trim());
        }
    }

    private void SwitchProvider(SttProvider provider)
    {
        _settingsService.UpdateProvider(provider);
        _activeSpeechService = provider == SttProvider.LocalWhisper
            ? _localService
            : _groqService;
        UpdateProviderMenuChecks();

        if (provider == SttProvider.LocalWhisper)
        {
            _ = PreloadWhisperModelAsync();
        }
    }

    private async Task PreloadWhisperModelAsync()
    {
        try
        {
            await _localService.PreloadAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MainWidget] Failed to preload Whisper model: {ex.Message}");
        }
    }

    private void UpdateProviderMenuChecks()
    {
        if (_contextMenu.Items["providerMenu"] is ToolStripMenuItem providerMenu)
        {
            foreach (ToolStripItem item in providerMenu.DropDownItems)
            {
                if (item is ToolStripMenuItem checkItem)
                {
                    checkItem.Checked = false;
                }
            }

            var activeProvider = _settingsService.Settings.ActiveProvider;
            var targetItem = activeProvider == SttProvider.GroqApi ? "Groq API (Nube)" : "Whisper Local (GPU/CPU)";

            foreach (ToolStripItem item in providerMenu.DropDownItems)
            {
                if (item is ToolStripMenuItem checkItem && checkItem.Text == targetItem)
                {
                    checkItem.Checked = true;
                }
            }
        }
    }

    private void ShowLocalModelDialog()
    {
        _topMostTimer.Stop();
        try
        {
            using var modelForm = new LocalModelForm(_settingsService);
            if (modelForm.ShowDialog(this) == DialogResult.OK)
            {
                _settingsService.UpdateLocalModel(modelForm.SelectedModelName, modelForm.UseGpu);
                _localService.ResetModel();
                _ = PreloadWhisperModelAsync();
            }
        }
        finally
        {
            _topMostTimer.Start();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_currentState == AppState.Listening)
        {
            _ = _audioCapture.StopRecordingAsync();
        }
        _topMostTimer.Stop();
        _topMostTimer.Dispose();
        _animationTimer.Stop();
        _animationTimer.Dispose();
        _audioCapture.AudioChunkCaptured -= OnAudioChunkCaptured;
        Deactivate -= OnDeactivate;
        if (_hotKeyService != null)
        {
            _hotKeyService.HotKeyPressed -= OnHotKeyPressed;
        }
        _trayIcon?.Dispose();
        _groqService.Dispose();
        _localService.Dispose();
        SavePosition();
        base.OnFormClosing(e);
    }

    private static GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
{
    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        using var brush = new SolidBrush(e.Item.Selected ? Color.FromArgb(60, 60, 60) : Color.FromArgb(30, 30, 30));
        e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(Color.FromArgb(30, 30, 30));
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        using var pen = new Pen(Color.FromArgb(50, 50, 50));
        var y = e.Item.Height / 2;
        e.Graphics.DrawLine(pen, 0, y, e.Item.Width, y);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(Color.FromArgb(30, 30, 30));
        e.Graphics.DrawRectangle(pen, 0, 0, e.AffectedBounds.Width - 1, e.AffectedBounds.Height - 1);
    }
}

internal static class GraphicsExtensions
{
    public static void FillRoundedRect(this Graphics g, Brush brush, int x, int y, int w, int h, int r)
    {
        using var path = new GraphicsPath();
        var d = r * 2;
        path.AddArc(x, y, d, d, 180, 90);
        path.AddArc(x + w - d, y, d, d, 270, 90);
        path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
        path.AddArc(x, y + h - d, d, d, 90, 90);
        path.CloseFigure();
        g.FillPath(brush, path);
    }
}
