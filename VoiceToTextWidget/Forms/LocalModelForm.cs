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
        ("large-v3-turbo", "Large V3 Turbo (~1.5 GB)", 1500L * 1024 * 1024),
    };

    public string SelectedModelName => Models[_modelCombo.SelectedIndex].Name;
    public bool UseGpu => _gpuCheckbox.Checked;

    public LocalModelForm(SettingsService settingsService)
    {
        _settingsService = settingsService;

        Text = "Configurar Modelo Local - Whisper";
        Size = new Size(480, 380);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(30, 30, 30);

        var lblTitle = new Label
        {
            Text = "Modelo de transcripcion local",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(15, 12),
            ForeColor = Color.White,
            BackColor = Color.Transparent
        };

        var lblModel = new Label
        {
            Text = "Modelo:",
            AutoSize = true,
            Location = new Point(15, 50),
            ForeColor = Color.White,
            BackColor = Color.Transparent
        };

        _modelCombo = new ComboBox
        {
            Location = new Point(15, 72),
            Width = 435,
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = Color.FromArgb(45, 45, 48),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
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
            Location = new Point(15, 100),
            ForeColor = Color.FromArgb(160, 160, 160),
            BackColor = Color.Transparent
        };

        _gpuCheckbox = new CheckBox
        {
            Text = "Usar aceleracion GPU (recomendado)",
            Location = new Point(15, 130),
            Width = 435,
            Checked = _settingsService.Settings.UseGpuAcceleration,
            ForeColor = Color.White,
            BackColor = Color.Transparent
        };

        _statusLabel = new Label
        {
            Text = "Estado: Verificando...",
            AutoSize = true,
            Location = new Point(15, 165),
            ForeColor = Color.FromArgb(120, 180, 255),
            BackColor = Color.Transparent
        };

        _downloadButton = new Button
        {
            Text = "Descargar modelo",
            Location = new Point(15, 200),
            Width = 140,
            Height = 35,
            BackColor = Color.FromArgb(70, 130, 180),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _downloadButton.Click += OnDownloadClick;

        _deleteButton = new Button
        {
            Text = "Eliminar modelo",
            Location = new Point(170, 200),
            Width = 140,
            Height = 35,
            BackColor = Color.FromArgb(180, 50, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Enabled = false
        };
        _deleteButton.Click += OnDeleteClick;

        _okButton = new Button
        {
            Text = "Guardar",
            Location = new Point(360, 290),
            Width = 90,
            Height = 35,
            BackColor = Color.FromArgb(70, 130, 180),
            ForeColor = Color.White,
            DialogResult = DialogResult.OK,
            FlatStyle = FlatStyle.Flat
        };

        _cancelButton = new Button
        {
            Text = "Cancelar",
            Location = new Point(260, 290),
            Width = 90,
            Height = 35,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White,
            DialogResult = DialogResult.Cancel,
            FlatStyle = FlatStyle.Flat
        };

        Controls.AddRange(new Control[]
        {
            lblTitle, lblModel, _modelCombo, _modelSizeLabel,
            _gpuCheckbox, _statusLabel, _downloadButton, _deleteButton,
            _okButton, _cancelButton
        });

        AcceptButton = _okButton;
        CancelButton = _cancelButton;

        UpdateModelStatus();
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
                _statusLabel.ForeColor = Color.FromArgb(160, 160, 160);
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
                _statusLabel.ForeColor = Color.FromArgb(160, 160, 160);
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
