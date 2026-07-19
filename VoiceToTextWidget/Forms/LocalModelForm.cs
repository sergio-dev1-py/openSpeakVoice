using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using VoiceToTextWidget.Models;
using VoiceToTextWidget.Services;

namespace VoiceToTextWidget.Forms;

public sealed class LocalModelForm : Form
{
    private readonly SettingsService _settingsService;
    private readonly ComboBox _modelCombo;
    private readonly CheckBox _gpuCheckbox;
    private readonly Label _statusLabel;
    private readonly Label _modelSizeLabel;
    private readonly Button _downloadButton;
    private readonly Button _deleteButton;
    private readonly Button _okButton;
    private readonly Button _cancelButton;
    private CancellationTokenSource? _downloadCts;
    private bool _isDownloading;

    private static readonly (string Name, string DisplayName, long SizeBytes)[] Models = new (string, string, long)[]
    {
        ("tiny", "Tiny (~75 MB)", 75L * 1024 * 1024),
        ("base", "Base (~142 MB)", 142L * 1024 * 1024),
        ("small", "Small (~466 MB)", 466L * 1024 * 1024),
        ("medium", "Medium (~1.5 GB)", 1500L * 1024 * 1024),
        ("large-v3", "Large V3 (~3.1 GB)", 3100L * 1024 * 1024),
    };

    public string SelectedModelName => Models[_modelCombo.SelectedIndex].Name;
    public bool UseGpu => _gpuCheckbox.Checked;

    public LocalModelForm(SettingsService settingsService)
    {
        _settingsService = settingsService;

        Text = "Configurar Modelo Local - Whisper";
        Size = new Size(520, 440);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(26, 26, 28);
        ForeColor = Color.FromArgb(220, 220, 225);
        Font = new Font("Segoe UI", 9.5f);

        var mainPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        var card = new Panel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.FromArgb(34, 34, 38),
            Padding = new Padding(16),
            MinimumSize = new Size(440, 0)
        };

        var cardContent = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        var lblTitle = new Label
        {
            Text = "Modelo de transcripcion local",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            AutoSize = true,
            ForeColor = Color.FromArgb(220, 220, 225),
            Margin = new Padding(0, 0, 0, 8)
        };

        var lblModel = new Label
        {
            Text = "Modelo:",
            AutoSize = true,
            ForeColor = Color.FromArgb(138, 138, 144),
            Font = new Font("Segoe UI", 9.5f),
            Margin = new Padding(0, 0, 0, 4)
        };

        _modelCombo = new ComboBox
        {
            Width = 400,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(45, 45, 50),
            ForeColor = Color.FromArgb(220, 220, 225),
            FlatStyle = FlatStyle.Flat,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 22,
            Margin = new Padding(0, 0, 0, 4)
        };
        _modelCombo.DrawItem += OnComboDrawItem;
        foreach (var model in Models)
        {
            _modelCombo.Items.Add(model.DisplayName);
        }

        var currentModelIndex = Array.FindIndex(Models, m =>
            m.Name == _settingsService.Settings.LocalModelName);
        _modelCombo.SelectedIndex = currentModelIndex >= 0 ? currentModelIndex : 2;
        _modelCombo.SelectedIndexChanged += OnModelChanged;

        _modelSizeLabel = new Label
        {
            Text = "",
            AutoSize = true,
            ForeColor = Color.FromArgb(138, 138, 144),
            Font = new Font("Segoe UI", 9f),
            Margin = new Padding(0, 0, 0, 8)
        };

        _gpuCheckbox = new CheckBox
        {
            Text = "Usar aceleracion GPU (recomendado)",
            Width = 400,
            Checked = _settingsService.Settings.UseGpuAcceleration,
            ForeColor = Color.FromArgb(200, 200, 210),
            Font = new Font("Segoe UI", 9.5f),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8),
            FlatStyle = FlatStyle.Flat
        };
        _gpuCheckbox.FlatAppearance.CheckedBackColor = Color.FromArgb(120, 170, 255);

        _statusLabel = new Label
        {
            Text = "Estado: Verificando...",
            AutoSize = true,
            ForeColor = Color.FromArgb(120, 170, 255),
            Font = new Font("Segoe UI", 9.5f),
            Margin = new Padding(0, 0, 0, 12)
        };

        var buttonRow = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        _downloadButton = CreateStyledButton("Descargar modelo", 150, Color.FromArgb(120, 170, 255));
        _downloadButton.Click += OnDownloadClick;
        _deleteButton = CreateStyledButton("Eliminar modelo", 150, Color.FromArgb(208, 64, 64));
        _deleteButton.Click += OnDeleteClick;
        _deleteButton.Enabled = false;
        buttonRow.Controls.Add(_downloadButton);
        buttonRow.Controls.Add(_deleteButton);

        cardContent.Controls.Add(lblTitle);
        cardContent.Controls.Add(lblModel);
        cardContent.Controls.Add(_modelCombo);
        cardContent.Controls.Add(_modelSizeLabel);
        cardContent.Controls.Add(_gpuCheckbox);
        cardContent.Controls.Add(_statusLabel);
        cardContent.Controls.Add(buttonRow);
        card.Controls.Add(cardContent);
        mainPanel.Controls.Add(card);

        var bottomPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Margin = new Padding(0, 8, 0, 0)
        };

        _okButton = CreateStyledButton("Guardar", 90, Color.FromArgb(120, 170, 255));
        _okButton.DialogResult = DialogResult.OK;
        _cancelButton = CreateStyledButton("Cancelar", 90, Color.FromArgb(60, 60, 65));
        _cancelButton.DialogResult = DialogResult.Cancel;
        bottomPanel.Controls.Add(_cancelButton);
        bottomPanel.Controls.Add(_okButton);
        mainPanel.Controls.Add(bottomPanel);

        Controls.Add(mainPanel);
        AcceptButton = _okButton;
        CancelButton = _cancelButton;

        UpdateModelStatus();
    }

    private static void OnComboDrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || sender is not ComboBox combo) return;
        string text = combo.Items[e.Index]?.ToString() ?? "";
        bool isSelected = (e.State & DrawItemState.Selected) != 0;
        var bgColor = isSelected ? Color.FromArgb(58, 58, 64) : Color.FromArgb(45, 45, 50);
        var textColor = isSelected ? Color.White : Color.FromArgb(210, 210, 218);

        using var bgBrush = new SolidBrush(bgColor);
        e.Graphics.FillRectangle(bgBrush, e.Bounds);
        using var textBrush = new SolidBrush(textColor);
        e.Graphics.DrawString(text, combo.Font, textBrush, e.Bounds.Left + 6, e.Bounds.Top + 3);
        e.DrawFocusRectangle();
    }

    private static Button CreateStyledButton(string text, int width, Color bgColor)
    {
        var button = new Button
        {
            Text = text,
            Size = new Size(width, 34),
            BackColor = bgColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    private void OnModelChanged(object? sender, EventArgs e)
    {
        UpdateModelStatus();
    }

    private void UpdateModelStatus()
    {
        if (_modelCombo.SelectedIndex < 0) return;

        var model = Models[_modelCombo.SelectedIndex];
        var modelPath = _settingsService.Settings.LocalModelPath
            .Replace($"ggml-{_settingsService.Settings.LocalModelName}.bin", $"ggml-{model.Name}.bin");

        var exists = File.Exists(modelPath);
        var sizeText = FormatSize(model.SizeBytes);

        _modelSizeLabel.Text = exists
            ? $"Tamano en disco: {sizeText} (descargado)"
            : $"Tamano: {sizeText} (no descargado)";

        _downloadButton.Enabled = !exists;
        _deleteButton.Enabled = exists;

        _statusLabel.Text = exists
            ? "Estado: Modelo listo"
            : "Estado: Modelo no descargado";
        _statusLabel.ForeColor = exists
            ? Color.FromArgb(80, 200, 80)
            : Color.FromArgb(200, 180, 80);
    }

    private async void OnDownloadClick(object? sender, EventArgs e)
    {
        if (_modelCombo.SelectedIndex < 0) return;

        var model = Models[_modelCombo.SelectedIndex];
        _isDownloading = true;
        _downloadButton.Enabled = false;
        _downloadButton.Text = "Descargando...";
        _okButton.Enabled = false;
        _cancelButton.Enabled = false;
        _modelCombo.Enabled = false;
        _deleteButton.Enabled = false;
        _statusLabel.Text = "Descargando modelo... esto puede tardar.";
        _statusLabel.ForeColor = Color.FromArgb(200, 180, 80);

        _downloadCts = new CancellationTokenSource();

        try
        {
            var modelDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "VoiceToTextWidget", "Models");
            Directory.CreateDirectory(modelDir);

            var modelPath = Path.Combine(modelDir, $"ggml-{model.Name}.bin");

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
            var url = $"https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-{model.Name}.bin";

            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, _downloadCts.Token);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(_downloadCts.Token);
            using var fileStream = File.Create(modelPath);
            await stream.CopyToAsync(fileStream, _downloadCts.Token);

            if (IsDisposed) return;

            _statusLabel.Text = "Descarga completada.";
            _statusLabel.ForeColor = Color.FromArgb(80, 200, 80);
            UpdateModelStatus();
        }
        catch (OperationCanceledException)
        {
            if (!IsDisposed)
            {
                _statusLabel.Text = "Descarga cancelada.";
                _statusLabel.ForeColor = Color.FromArgb(120, 120, 120);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LocalModel] Download error: {ex.Message}");
            if (!IsDisposed)
            {
                _statusLabel.Text = $"Error: {ex.Message}";
                _statusLabel.ForeColor = Color.FromArgb(200, 80, 80);
            }
        }
        finally
        {
            _downloadCts?.Dispose();
            _downloadCts = null;
            _isDownloading = false;

            if (!IsDisposed)
            {
                _downloadButton.Text = "Descargar modelo";
                _okButton.Enabled = true;
                _cancelButton.Enabled = true;
                _modelCombo.Enabled = true;
                UpdateModelStatus();
            }
        }
    }

    private void OnDeleteClick(object? sender, EventArgs e)
    {
        if (_modelCombo.SelectedIndex < 0) return;

        var model = Models[_modelCombo.SelectedIndex];
        var modelPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VoiceToTextWidget", "Models", $"ggml-{model.Name}.bin");

        if (!File.Exists(modelPath)) return;

        var result = MessageBox.Show(
            $"Eliminar el modelo {model.DisplayName}?\nSe liberara espacio en disco.",
            "Confirmar eliminacion",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            try
            {
                File.Delete(modelPath);
                _statusLabel.Text = "Modelo eliminado.";
                _statusLabel.ForeColor = Color.FromArgb(120, 120, 120);
                UpdateModelStatus();
            }
            catch (Exception ex)
            {
                _statusLabel.Text = $"Error al eliminar: {ex.Message}";
                _statusLabel.ForeColor = Color.FromArgb(200, 80, 80);
            }
        }
    }

    private static string FormatSize(long bytes)
    {
        if (bytes >= 1024L * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
        if (bytes >= 1024L * 1024)
            return $"{bytes / (1024.0 * 1024):F0} MB";
        return $"{bytes / 1024.0:F0} KB";
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_isDownloading)
        {
            _downloadCts?.Cancel();
        }
        base.OnFormClosing(e);
    }
}
