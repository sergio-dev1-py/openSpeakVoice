using System.Diagnostics;
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
        Size = new Size(500, 400);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(30, 30, 30);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9);

        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24),
            ColumnCount = 1,
            RowCount = 9,
            BackColor = Color.Transparent,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        for (int i = 0; i < 9; i++)
            mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainPanel.RowStyles[8] = new RowStyle(SizeType.Percent, 100);

        var lblTitle = new Label
        {
            Text = "Modelo de transcripcion local",
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            AutoSize = true,
            ForeColor = Color.White,
            Margin = new Padding(0, 0, 0, 8)
        };

        var lblModel = new Label
        {
            Text = "Modelo:",
            AutoSize = true,
            ForeColor = Color.FromArgb(120, 120, 120),
            Margin = new Padding(0, 0, 0, 4)
        };

        _modelCombo = new ComboBox
        {
            Width = 440,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(45, 45, 48),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Standard,
            Margin = new Padding(0, 0, 0, 4)
        };
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
            ForeColor = Color.FromArgb(120, 120, 120),
            Margin = new Padding(0, 0, 0, 8)
        };

        _gpuCheckbox = new CheckBox
        {
            Text = "Usar aceleracion GPU (recomendado)",
            Width = 440,
            Checked = _settingsService.Settings.UseGpuAcceleration,
            ForeColor = Color.White,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8)
        };

        _statusLabel = new Label
        {
            Text = "Estado: Verificando...",
            AutoSize = true,
            ForeColor = Color.FromArgb(79, 110, 247),
            Margin = new Padding(0, 0, 0, 12)
        };

        var buttonPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 8)
        };

        _downloadButton = CreateStyledButton("Descargar modelo", 150, Color.FromArgb(79, 110, 247));
        _downloadButton.Click += OnDownloadClick;
        _deleteButton = CreateStyledButton("Eliminar modelo", 150, Color.FromArgb(180, 50, 50));
        _deleteButton.Click += OnDeleteClick;
        _deleteButton.Enabled = false;
        buttonPanel.Controls.Add(_downloadButton);
        buttonPanel.Controls.Add(_deleteButton);

        var bottomPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new Padding(0, 12, 0, 0)
        };

        _okButton = CreateStyledButton("Guardar", 90, Color.FromArgb(79, 110, 247));
        _okButton.DialogResult = DialogResult.OK;
        _cancelButton = CreateStyledButton("Cancelar", 90, Color.FromArgb(60, 60, 65));
        _cancelButton.DialogResult = DialogResult.Cancel;
        bottomPanel.Controls.Add(_okButton);
        bottomPanel.Controls.Add(_cancelButton);

        mainPanel.Controls.Add(lblTitle, 0, 0);
        mainPanel.Controls.Add(lblModel, 0, 1);
        mainPanel.Controls.Add(_modelCombo, 0, 2);
        mainPanel.Controls.Add(_modelSizeLabel, 0, 3);
        mainPanel.Controls.Add(_gpuCheckbox, 0, 4);
        mainPanel.Controls.Add(_statusLabel, 0, 5);
        mainPanel.Controls.Add(buttonPanel, 0, 6);
        mainPanel.Controls.Add(new Panel { Height = 20 }, 0, 7);
        mainPanel.Controls.Add(bottomPanel, 0, 8);

        Controls.Add(mainPanel);
        AcceptButton = _okButton;
        CancelButton = _cancelButton;

        UpdateModelStatus();
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
