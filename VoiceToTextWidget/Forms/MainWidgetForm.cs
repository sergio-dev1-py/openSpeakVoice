using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Microsoft.Win32;
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
    private ToolStripMenuItem? _translateMenu;

    private int _pulseAlpha = 180;
    private int _pulseDirection = 1;
    private int _animFrame;
    private int _animTick;
    private float _colorHue = 0f;

    private readonly float[] _waveformBuffer = new float[12];
    private float _currentAmplitude;
    private Icon? _appIcon;

    private const int WidgetWidth = 63;
    private const int WidgetHeight = 27;
    private const int CornerRadius = 10;
    private const int BorderWidth = 2;

    private static readonly Color ColorIdle = Color.FromArgb(45, 45, 48);
    private static readonly Color ColorListening = Color.FromArgb(200, 50, 50);
    private static readonly Color ColorTranscribing = Color.FromArgb(200, 140, 40);

    private string CurrentLang => _settingsService.Settings.AppLanguage;

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

        var icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "app.ico");
        if (File.Exists(icoPath))
        {
            _appIcon = new Icon(icoPath, 32, 32);
            Icon = (Icon)_appIcon.Clone();
        }
    }

    private void InitTrayIcon()
    {
        _trayIcon = new NotifyIcon
        {
            Icon = CreateAdaptiveTrayIcon(),
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

    private static Icon CreateAdaptiveTrayIcon()
    {
        bool isDarkTheme = true;
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int val)
                isDarkTheme = val == 0;
        }
        catch { }

        Color bgColor = isDarkTheme ? Color.White : Color.FromArgb(34, 34, 38);
        Color micColor = isDarkTheme ? Color.FromArgb(34, 34, 38) : Color.White;
        Color borderColor = isDarkTheme ? Color.FromArgb(180, 180, 180) : Color.FromArgb(80, 80, 85);

        using var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        using var bgBrush = new SolidBrush(bgColor);
        using var bgPath = GetRoundedRectPath(new Rectangle(1, 1, 14, 14), 4);
        g.FillPath(bgBrush, bgPath);

        using var borderPen = new Pen(borderColor, 1f);
        g.DrawPath(borderPen, bgPath);

        using var pen = new Pen(micColor, 1.5f);
        using var brush = new SolidBrush(micColor);

        g.FillRoundedRect(brush, 6, 3, 4, 8, 2);

        using var arcPath = new GraphicsPath();
        arcPath.AddArc(4, 2, 8, 6, 180, 180);
        arcPath.CloseFigure();
        g.DrawPath(pen, arcPath);

        g.DrawLine(pen, 8, 11, 8, 14);
        g.DrawLine(pen, 5, 14, 11, 14);

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
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.CompositingQuality = CompositingQuality.HighQuality;

        var bgColor = GetAnimatedBackground();
        var borderColor = GetBorderColor();
        var bw = BorderWidth;

        var outerRect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var outerPath = GetRoundedRectPath(outerRect, CornerRadius);
        using var borderBrush = new SolidBrush(borderColor);
        g.FillPath(borderBrush, outerPath);

        var innerRect = new Rectangle(bw, bw, Width - 1 - bw * 2, Height - 1 - bw * 2);
        using var innerPath = GetRoundedRectPath(innerRect, Math.Max(1, CornerRadius - bw));
        using var bgBrush = new SolidBrush(bgColor);
        g.FillPath(bgBrush, innerPath);

        Region = new Region(outerPath);

        using var overlayPen = new Pen(borderColor, 2f);
        g.DrawPath(overlayPen, outerPath);

        DrawMicIcon(g, innerRect);
        DrawWaveform(g, innerRect);
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

        return Color.FromArgb(_settingsService.Settings.BorderOpacity, userColor.R, userColor.G, userColor.B);
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
        int iconBoxSize = 20;
        int cx = bounds.Left + 5 + iconBoxSize / 2;
        int cy = bounds.Top + (bounds.Height - iconBoxSize) / 2;

        Color boxColor = _currentState == AppState.Idle
            ? Color.White
            : ColorTranslator.FromHtml(_settingsService.Settings.BorderColor);
        using var boxBrush = new SolidBrush(boxColor);
        using var boxPath = GetRoundedRectPath(
            new Rectangle(cx - iconBoxSize / 2, cy, iconBoxSize, iconBoxSize), 5);
        g.FillPath(boxBrush, boxPath);

        if (_appIcon != null)
        {
            using var iconBitmap = _appIcon.ToBitmap();
            g.DrawImage(iconBitmap, new Rectangle(cx - 9, cy + 1, 18, 18));
        }
        else
        {
            Color micColor = Color.FromArgb(200, 200, 200);
            using var pen = new Pen(micColor, 1.5f);
            using var brush = new SolidBrush(micColor);

            int micCx = cx;
            int micCy = cy + iconBoxSize / 2;

            g.FillRoundedRect(brush, micCx - 3, micCy - 5, 6, 9, 3);

            using var arcPath = new GraphicsPath();
            arcPath.AddArc(micCx - 5, micCy - 8, 10, 8, 180, 180);
            arcPath.CloseFigure();
            g.DrawPath(pen, arcPath);

            g.DrawLine(pen, micCx, micCy + 1, micCx, micCy + 4);
            g.DrawLine(new Pen(micColor, 1.2f), micCx - 3, micCy + 4, micCx + 3, micCy + 4);
        }
    }

    private void DrawWaveform(Graphics g, Rectangle bounds)
    {
        int barCount = _waveformBuffer.Length;
        int barWidth = 2;
        int gap = 2;
        int micAreaRight = bounds.Left + 28;
        int availableWidth = bounds.Right - micAreaRight;
        int totalBarsWidth = barCount * (barWidth + gap);
        int startX = micAreaRight + Math.Max(0, (availableWidth - totalBarsWidth) / 2);
        int centerY = bounds.Top + bounds.Height / 2;
        int maxBarHeight = 8;

        if (_currentState == AppState.Transcribing)
        {
            DrawSpinner(g, bounds);
            return;
        }

        float amp = Math.Max(_currentAmplitude, 0.3f);

        Color barColor;
        if (_currentState == AppState.Listening)
        {
            barColor = GetBorderColor();
        }
        else
        {
            int alpha = _pulseAlpha;
            barColor = Color.FromArgb(Math.Clamp(alpha, 60, 200), 200, 200, 200);
        }
        using var pen = new Pen(barColor, barWidth);

        float mid = (barCount - 1) / 2f;
        for (int i = 0; i < barCount; i++)
        {
            float dist = Math.Abs(i - mid) / mid;
            float weight = (float)Math.Cos(dist * Math.PI / 3);
            float barAmp = amp * weight;
            int h = Math.Max(1, (int)(barAmp * maxBarHeight));
            int x = startX + i * (barWidth + gap);
            g.DrawLine(pen, x, centerY - h / 2, x, centerY + h / 2);
        }
    }

    private void DrawSpinner(Graphics g, Rectangle bounds)
    {
        int size = 12;
        int micAreaRight = bounds.Left + 28;
        int availableWidth = bounds.Right - micAreaRight;
        int centerX = micAreaRight + availableWidth / 2;
        int centerY = bounds.Top + bounds.Height / 2;

        var rect = new Rectangle(centerX - size / 2, centerY - size / 2, size, size);
        float startAngle = (_animFrame * 15f) % 360;

        using var pen = new Pen(Color.FromArgb(200, 200, 200), 2f);
        g.DrawArc(pen, rect, startAngle, 270);

        float endAngle = (startAngle + 270) * MathF.PI / 180f;
        float dotX = centerX + (size / 2f) * MathF.Cos(endAngle);
        float dotY = centerY + (size / 2f) * MathF.Sin(endAngle);
        g.FillEllipse(Brushes.White, dotX - 1.5f, dotY - 1.5f, 3, 3);
    }

    private void OnAnimationTick(object? sender, EventArgs e)
    {
        bool needsInvalidate = false;

        if (_settingsService.Settings.MulticolorBorder)
        {
            _colorHue += 1.5f;
            if (_colorHue >= 360f) _colorHue -= 360f;
            needsInvalidate = true;
        }

        if (_currentState == AppState.Listening)
        {
            _pulseAlpha += 8 * _pulseDirection;
            if (_pulseAlpha >= 255) { _pulseAlpha = 255; _pulseDirection = -1; }
            if (_pulseAlpha <= 140) { _pulseAlpha = 140; _pulseDirection = 1; }

            _currentAmplitude *= 0.92f;
            if (_currentAmplitude < 0.01f) _currentAmplitude = 0f;
            needsInvalidate = true;
        }
        else if (_currentState == AppState.Transcribing)
        {
            _animTick++;
            if (_animTick >= 2)
            {
                _animTick = 0;
                _animFrame++;
                needsInvalidate = true;
            }
        }
        else if (_currentState == AppState.Idle)
        {
            _currentAmplitude = 0.02f;
            needsInvalidate = true;
        }

        if (needsInvalidate) Invalidate();
    }

    private ContextMenuStrip SetupContextMenu()
    {
        var lang = CurrentLang;
        var contextMenu = new ContextMenuStrip
        {
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.White,
            Renderer = new DarkMenuRenderer()
        };

        var settingsItem = new ToolStripMenuItem(WidgetStrings.Get("menu_config", lang))
        {
            ForeColor = Color.White,
            BackColor = Color.FromArgb(30, 30, 30)
        };
        settingsItem.Click += (_, _) => ShowSettingsDialog();

        var apiKeyItem = new ToolStripMenuItem(WidgetStrings.Get("menu_apikeys", lang))
        {
            ForeColor = Color.White,
            BackColor = Color.FromArgb(30, 30, 30)
        };
        apiKeyItem.Click += (_, _) => ShowApiKeyDialog();

        var providerMenu = new ToolStripMenuItem(WidgetStrings.Get("menu_provider", lang))
        {
            Name = "providerMenu",
            ForeColor = Color.White,
            BackColor = Color.FromArgb(30, 30, 30)
        };

        var groqItem = new ToolStripMenuItem(WidgetStrings.Get("menu_groq", lang))
        {
            ForeColor = Color.White,
            BackColor = Color.FromArgb(30, 30, 30),
            Checked = _settingsService.Settings.ActiveProvider == SttProvider.GroqApi
        };
        groqItem.Click += (_, _) => SwitchProvider(SttProvider.GroqApi);

        var localItem = new ToolStripMenuItem(WidgetStrings.Get("menu_local", lang))
        {
            ForeColor = Color.White,
            BackColor = Color.FromArgb(30, 30, 30),
            Checked = _settingsService.Settings.ActiveProvider == SttProvider.LocalWhisper
        };
        localItem.Click += (_, _) => SwitchProvider(SttProvider.LocalWhisper);

        var modelConfigItem = new ToolStripMenuItem(WidgetStrings.Get("menu_model", lang))
        {
            ForeColor = Color.White,
            BackColor = Color.FromArgb(30, 30, 30)
        };
        modelConfigItem.Click += (_, _) => ShowLocalModelDialog();

        providerMenu.DropDownItems.AddRange(new ToolStripItem[] { groqItem, localItem, modelConfigItem });

        var modeMenu = new ToolStripMenuItem(WidgetStrings.Get("menu_mode", lang))
        {
            Name = "modeMenu",
            ForeColor = Color.White,
            BackColor = Color.FromArgb(30, 30, 30)
        };

        var transcribeItem = new ToolStripMenuItem(WidgetStrings.Get("mode_transcription", lang))
        {
            ForeColor = Color.White,
            BackColor = Color.FromArgb(30, 30, 30),
            Checked = _settingsService.Settings.Mode == "transcription"
        };
        transcribeItem.Click += (_, _) => SwitchMode("transcription");

        var translateItem = new ToolStripMenuItem(WidgetStrings.Get("mode_translation", lang))
        {
            ForeColor = Color.White,
            BackColor = Color.FromArgb(30, 30, 30),
            Checked = _settingsService.Settings.Mode == "translation"
        };
        translateItem.Click += (_, _) => SwitchMode("translation");

        modeMenu.DropDownItems.AddRange(new ToolStripItem[] { transcribeItem, translateItem });

        _translateMenu = new ToolStripMenuItem(WidgetStrings.Get("menu_target_lang", lang))
        {
            Name = "translateMenu",
            ForeColor = Color.White,
            BackColor = Color.FromArgb(30, 30, 30),
            Visible = _settingsService.Settings.Mode == "translation"
        };

        foreach (var targetLang in AppLanguages.CommonLanguages)
        {
            var item = new ToolStripMenuItem($"{targetLang.DisplayName} ({targetLang.Code})")
            {
                ForeColor = Color.White,
                BackColor = Color.FromArgb(30, 30, 30),
                Tag = targetLang.Code,
                Checked = _settingsService.Settings.TargetLanguage == targetLang.Code
            };
            item.Click += (_, _) => SwitchTargetLanguage(targetLang.Code);
            _translateMenu.DropDownItems.Add(item);
        }

        _hideMenuItem = new ToolStripMenuItem(WidgetStrings.Get("menu_hide", lang))
        {
            ForeColor = Color.White,
            BackColor = Color.FromArgb(30, 30, 30),
            Enabled = false
        };
        _hideMenuItem.Click += (_, _) => HideWidget();

        _exitMenuItem = new ToolStripMenuItem(WidgetStrings.Get("menu_exit", lang))
        {
            ForeColor = Color.White,
            BackColor = Color.FromArgb(30, 30, 30)
        };
        _exitMenuItem.Click += (_, _) => Application.Exit();

        contextMenu.Items.AddRange(new ToolStripItem[]
        {
            settingsItem, apiKeyItem, providerMenu, modeMenu, _translateMenu,
            _hideMenuItem, _exitMenuItem
        });

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
                WinApi.SWP_NOMOVE | WinApi.SWP_NOSIZE | WinApi.SWP_NOACTIVATE);
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

                if (_settingsService.Settings.Mode == "translation"
                    && _activeSpeechService == _groqService)
                {
                    text = await _groqService.TranslateTextAsync(text, _settingsService.Settings.TargetLanguage);
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
        if (_currentState != AppState.Listening || bytesRecorded < 2) return;

        float peak = 0f;
        int samples = bytesRecorded / 2;
        for (int i = 0; i < samples; i++)
        {
            short sample = (short)(buffer[i * 2] | (buffer[i * 2 + 1] << 8));
            float normalized = Math.Abs(sample / 32768f);
            if (normalized > peak) peak = normalized;
        }

        _currentAmplitude = Math.Min(peak * 3.5f, 1f);
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
        _topMostTimer.Stop();
        try
        {
            using var settingsForm = new SettingsForm(_settingsService.Settings);
            if (settingsForm.ShowDialog(this) == DialogResult.OK)
            {
                _settingsService.UpdateHotKey(settingsForm.HotKeyValue, settingsForm.SelectedModifiers);
                _settingsService.UpdateAppearance(settingsForm.BorderColorHex, settingsForm.MulticolorBorder, settingsForm.BorderOpacity);
                _settingsService.UpdateAppLanguage(settingsForm.SelectedLanguage);
                _settingsService.UpdateMode(settingsForm.SelectedMode);
                _settingsService.UpdateTargetLanguage(settingsForm.SelectedTargetLanguage);

                Invalidate();
                _hotKeyService?.Reregister();
                RefreshContextMenu();

                if (_hotKeyService != null && !_hotKeyService.IsRegistered)
                {
                    ShowError("No se pudo registrar el nuevo atajo de teclado.");
                }
            }
        }
        finally
        {
            _topMostTimer.Start();
        }
    }

    private void ShowApiKeyDialog()
    {
        _topMostTimer.Stop();
        try
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
                BackColor = Color.FromArgb(26, 26, 28),
                ForeColor = Color.FromArgb(220, 220, 225),
                Font = new Font("Segoe UI", 9.5f)
            };

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                ColumnCount = 1,
                RowCount = 5,
                BackColor = Color.Transparent
            };

            var title = new Label
            {
                Text = "Configurar API Keys de Groq",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 220, 225),
                Margin = new Padding(0, 0, 0, 4)
            };

            var subtitle = new Label
            {
                Text = "Obtén keys gratis en: https://console.groq.com",
                AutoSize = true,
                ForeColor = Color.FromArgb(120, 170, 255),
                Font = new Font("Segoe UI", 8.5f),
                Margin = new Padding(0, 0, 0, 12)
            };

            var card = new Panel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.FromArgb(34, 34, 38),
                Padding = new Padding(16)
            };

            var cardFlow = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            var lblKey1Status = new Label
            {
                Text = $"Key 1: {masked1}  |  Usados: {used1}/{total1}",
                AutoSize = true,
                ForeColor = Color.FromArgb(200, 200, 210),
                Margin = new Padding(0, 0, 0, 4)
            };

            var txtKey1 = new TextBox
            {
                Width = 370,
                UseSystemPasswordChar = true,
                BackColor = Color.FromArgb(45, 45, 50),
                ForeColor = Color.FromArgb(220, 220, 225),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f),
                Text = s.ApiKey1,
                Margin = new Padding(0, 0, 0, 12)
            };

            var lblKey2Status = new Label
            {
                Text = $"Key 2: {masked2}  |  Usados: {used2}/{total2}",
                AutoSize = true,
                ForeColor = Color.FromArgb(200, 200, 210),
                Margin = new Padding(0, 0, 0, 4)
            };

            var txtKey2 = new TextBox
            {
                Width = 370,
                UseSystemPasswordChar = true,
                BackColor = Color.FromArgb(45, 45, 50),
                ForeColor = Color.FromArgb(220, 220, 225),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f),
                Text = s.ApiKey2,
                Margin = new Padding(0, 0, 0, 6)
            };

            var lblInfo = new Label
            {
                Text = "Las keys se guardan automaticamente.\nRotacion automatica al llegar a 19 req/min por key.",
                AutoSize = true,
                ForeColor = Color.FromArgb(120, 120, 125),
                Font = new Font("Segoe UI", 8.5f)
            };

            cardFlow.Controls.Add(lblKey1Status);
            cardFlow.Controls.Add(txtKey1);
            cardFlow.Controls.Add(lblKey2Status);
            cardFlow.Controls.Add(txtKey2);
            cardFlow.Controls.Add(lblInfo);
            card.Controls.Add(cardFlow);

            var okButton = new Button
            {
                Text = "Guardar",
                Size = new Size(90, 32),
                BackColor = Color.FromArgb(120, 170, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK,
                Cursor = Cursors.Hand
            };
            okButton.FlatAppearance.BorderSize = 0;

            var cancelButton = new Button
            {
                Text = "Cancelar",
                Size = new Size(90, 32),
                BackColor = Color.FromArgb(60, 60, 65),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel,
                Cursor = Cursors.Hand
            };
            cancelButton.FlatAppearance.BorderSize = 0;

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Margin = new Padding(0, 12, 0, 0)
            };
            buttonPanel.Controls.Add(cancelButton);
            buttonPanel.Controls.Add(okButton);

            mainPanel.Controls.Add(title, 0, 0);
            mainPanel.Controls.Add(subtitle, 0, 1);
            mainPanel.Controls.Add(card, 0, 2);
            mainPanel.Controls.Add(new Panel { Height = 0 }, 0, 3);
            mainPanel.Controls.Add(buttonPanel, 0, 4);

            inputForm.Controls.Add(mainPanel);
            inputForm.AcceptButton = okButton;
            inputForm.CancelButton = cancelButton;

            if (inputForm.ShowDialog(this) == DialogResult.OK)
            {
                _settingsService.UpdateApiKey1(txtKey1.Text.Trim());
                _settingsService.UpdateApiKey2(txtKey2.Text.Trim());
            }
        }
        finally
        {
            _topMostTimer.Start();
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

    private void SwitchMode(string mode)
    {
        _settingsService.UpdateMode(mode);
        if (_translateMenu != null)
            _translateMenu.Visible = mode == "translation";
        UpdateModeMenuChecks();
    }

    private void SwitchTargetLanguage(string code)
    {
        _settingsService.UpdateTargetLanguage(code);
        UpdateTargetLangMenuChecks();
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
                    checkItem.Checked = false;
            }

            var activeProvider = _settingsService.Settings.ActiveProvider;
            var targetItem = activeProvider == SttProvider.GroqApi
                ? WidgetStrings.Get("menu_groq", CurrentLang)
                : WidgetStrings.Get("menu_local", CurrentLang);

            foreach (ToolStripItem item in providerMenu.DropDownItems)
            {
                if (item is ToolStripMenuItem checkItem && checkItem.Text == targetItem)
                    checkItem.Checked = true;
            }
        }
    }

    private void UpdateModeMenuChecks()
    {
        if (_contextMenu.Items["modeMenu"] is ToolStripMenuItem modeMenu)
        {
            foreach (ToolStripItem item in modeMenu.DropDownItems)
            {
                if (item is ToolStripMenuItem checkItem)
                    checkItem.Checked = false;
            }

            var activeMode = _settingsService.Settings.Mode;
            var targetItem = activeMode == "transcription"
                ? WidgetStrings.Get("mode_transcription", CurrentLang)
                : WidgetStrings.Get("mode_translation", CurrentLang);

            foreach (ToolStripItem item in modeMenu.DropDownItems)
            {
                if (item is ToolStripMenuItem checkItem && checkItem.Text == targetItem)
                    checkItem.Checked = true;
            }
        }
    }

    private void UpdateTargetLangMenuChecks()
    {
        if (_translateMenu == null) return;

        foreach (ToolStripItem item in _translateMenu.DropDownItems)
        {
            if (item is ToolStripMenuItem checkItem)
                checkItem.Checked = false;
        }

        var targetCode = _settingsService.Settings.TargetLanguage;
        foreach (ToolStripItem item in _translateMenu.DropDownItems)
        {
            if (item is ToolStripMenuItem checkItem && checkItem.Tag is string code && code == targetCode)
                checkItem.Checked = true;
        }
    }

    private void RefreshContextMenu()
    {
        var lang = CurrentLang;

        if (_contextMenu.Items["providerMenu"] is ToolStripMenuItem providerMenu)
            providerMenu.Text = WidgetStrings.Get("menu_provider", lang);

        if (_contextMenu.Items["modeMenu"] is ToolStripMenuItem modeMenu)
            modeMenu.Text = WidgetStrings.Get("menu_mode", lang);

        if (_translateMenu != null)
        {
            _translateMenu.Text = WidgetStrings.Get("menu_target_lang", lang);
            _translateMenu.Visible = _settingsService.Settings.Mode == "translation";
        }

        if (_hideMenuItem != null)
            _hideMenuItem.Text = WidgetStrings.Get("menu_hide", lang);

        if (_exitMenuItem != null)
            _exitMenuItem.Text = WidgetStrings.Get("menu_exit", lang);

        foreach (ToolStripMenuItem item in _contextMenu.Items)
        {
            if (item.HasDropDownItems)
            {
                foreach (ToolStripItem subItem in item.DropDownItems)
                {
                    if (subItem.Text == WidgetStrings.Get("menu_groq", lang) ||
                        subItem.Text == WidgetStrings.Get("menu_local", lang) ||
                        subItem.Text == WidgetStrings.Get("menu_model", lang) ||
                        subItem.Text == WidgetStrings.Get("mode_transcription", lang) ||
                        subItem.Text == WidgetStrings.Get("mode_translation", lang))
                    {
                        // these are set by their respective update methods
                    }
                }
            }
        }

        UpdateProviderMenuChecks();
        UpdateModeMenuChecks();
        UpdateTargetLangMenuChecks();
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
    private static readonly Color BgColor = Color.FromArgb(26, 26, 28);
    private static readonly Color HoverColor = Color.FromArgb(58, 58, 64);
    private static readonly Color BorderColor = Color.FromArgb(40, 40, 44);
    private static readonly Color SeparatorColor = Color.FromArgb(51, 51, 56);
    private static readonly Color TextColor = Color.FromArgb(210, 210, 218);
    private static readonly Color CheckColor = Color.FromArgb(120, 170, 255);

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var color = e.Item.Selected ? HoverColor : BgColor;
        using var brush = new SolidBrush(color);
        e.Graphics.FillRectangle(brush, new Rectangle(0, 0, e.Item.Width, e.Item.Height));
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Selected ? Color.White : TextColor;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(BgColor);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        var y = e.Item.Height / 2;
        using var pen = new Pen(SeparatorColor);
        e.Graphics.DrawLine(pen, 4, y, e.Item.Width - 8, y);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(BorderColor);
        e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, e.AffectedBounds.Width - 1, e.AffectedBounds.Height - 1));
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        e.ArrowColor = Color.FromArgb(140, 140, 145);
        base.OnRenderArrow(e);
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {
        using var brush = new SolidBrush(CheckColor);
        e.Graphics.FillRectangle(brush, new Rectangle(2, 2, 14, 14));
        using var pen = new Pen(Color.White, 1.5f);
        e.Graphics.DrawLine(pen, 4, 9, 7, 12);
        e.Graphics.DrawLine(pen, 7, 12, 13, 5);
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
