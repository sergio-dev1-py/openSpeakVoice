using System.Diagnostics;
using System.Text;
using Whisper.net;
using Whisper.net.Ggml;
using Whisper.net.LibraryLoader;

namespace VoiceToTextWidget.Services;

public sealed class WhisperLocalSpeechService : ISpeechRecognitionService
{
    private readonly SettingsService _settings;
    private WhisperFactory? _whisperFactory;
    private bool _initialized;
    private bool _disposed;

    private const int SampleRate = 16000;
    private const int MinAudioBytes = SampleRate * 2 / 4;

    public WhisperLocalSpeechService(SettingsService settings)
    {
        _settings = settings;
    }

    public async Task PreloadAsync()
    {
        if (_initialized) return;

        var modelPath = _settings.Settings.LocalModelPath;

        try
        {
            if (!File.Exists(modelPath))
            {
                Debug.WriteLine($"[Whisper] Model not found at {modelPath}, downloading...");
                await DownloadModelAsync(modelPath);
            }

            var modelSizeMB = new FileInfo(modelPath).Length / (1024L * 1024);
            Debug.WriteLine($"[Whisper] Loading model: {modelPath} ({modelSizeMB} MB)");
            Debug.WriteLine($"[Whisper] Runtime order: {string.Join(", ", RuntimeOptions.RuntimeLibraryOrder)}");
            Debug.WriteLine($"[Whisper] Loaded library: {RuntimeOptions.LoadedLibrary}");
            Debug.WriteLine($"[Whisper] UseGpu: {_settings.Settings.UseGpuAcceleration}");

            try
            {
                _whisperFactory = WhisperFactory.FromPath(modelPath, new WhisperFactoryOptions
                {
                    UseGpu = false
                });
            }
            catch (WhisperModelLoadException wex)
            {
                Debug.WriteLine($"[Whisper] WhisperModelLoadException: {wex.Message}");
                if (wex.InnerException != null)
                    Debug.WriteLine($"[Whisper] Inner: {wex.InnerException.GetType().Name}: {wex.InnerException.Message}");
                if (wex.InnerException?.InnerException != null)
                    Debug.WriteLine($"[Whisper] Inner2: {wex.InnerException.InnerException.GetType().Name}: {wex.InnerException.InnerException.Message}");
                throw;
            }

            _initialized = true;
            Debug.WriteLine("[Whisper] Model loaded successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Whisper] FAILED: {ex.GetType().Name}: {ex.Message}");
            Debug.WriteLine($"[Whisper] Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
                Debug.WriteLine($"[Whisper] Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            throw;
        }
    }

    public Task StartSessionAsync()
    {
        return Task.CompletedTask;
    }

    public void FeedAudio(byte[] buffer, int bytesRecorded)
    {
    }

    public async Task<string> FinishSessionAsync(byte[] audioData)
    {
        if (audioData.Length < MinAudioBytes)
        {
            return string.Empty;
        }

        if (!_initialized || _whisperFactory == null)
        {
            await PreloadAsync();
        }

        if (_whisperFactory == null)
        {
            throw new InvalidOperationException("Whisper model not loaded");
        }

        try
        {
            var language = _settings.Settings.Language;

            using var processor = _whisperFactory.CreateBuilder()
                .WithLanguage(language)
                .WithTokenTimestamps()
                .WithTemperature(0.0f)
                .Build();

            using var wavStream = ConvertPcmToWav(audioData);

            var results = new List<string>();
            await foreach (var segment in processor.ProcessAsync(wavStream))
            {
                results.Add(segment.Text);
            }

            var text = string.Join(" ", results).Trim();
            Debug.WriteLine($"[Whisper] Transcription: {text}");
            return text;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Whisper] Transcription error: {ex.Message}");
            throw;
        }
    }

    public Task<string> FinishSessionAsync()
    {
        return Task.FromResult(string.Empty);
    }

    private async Task DownloadModelAsync(string modelPath)
    {
        var modelName = _settings.Settings.LocalModelName;
        var ggmlType = modelName switch
        {
            "tiny" => GgmlType.Tiny,
            "base" => GgmlType.Base,
            "small" => GgmlType.Small,
            "medium" => GgmlType.Medium,
            "large-v3" => GgmlType.LargeV3,
            _ => GgmlType.Small
        };

        var modelDir = Path.GetDirectoryName(modelPath);
        if (!string.IsNullOrEmpty(modelDir))
        {
            Directory.CreateDirectory(modelDir);
        }

        Debug.WriteLine($"[Whisper] Downloading model {modelName}...");
        using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(ggmlType);
        using var fileWriter = File.OpenWrite(modelPath);
        await modelStream.CopyToAsync(fileWriter);
        Debug.WriteLine($"[Whisper] Model downloaded to {modelPath}");
    }

    private static MemoryStream ConvertPcmToWav(byte[] pcmData)
    {
        var wavStream = new MemoryStream();
        var channels = 1;
        var bitsPerSample = 16;
        var byteRate = SampleRate * channels * bitsPerSample / 8;
        var blockAlign = channels * bitsPerSample / 8;
        var dataSize = pcmData.Length;

        using (var writer = new BinaryWriter(wavStream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataSize);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));

            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(SampleRate);
            writer.Write(byteRate);
            writer.Write((short)blockAlign);
            writer.Write((short)bitsPerSample);

            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);
            writer.Write(pcmData);
        }

        wavStream.Seek(0, SeekOrigin.Begin);
        return wavStream;
    }

    public void ResetModel()
    {
        _whisperFactory?.Dispose();
        _whisperFactory = null;
        _initialized = false;
        Debug.WriteLine("[Whisper] Model reset, will reload on next session");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _whisperFactory?.Dispose();
    }
}
