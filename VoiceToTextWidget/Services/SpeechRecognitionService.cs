using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace VoiceToTextWidget.Services;

public sealed class SpeechRecognitionService : IDisposable
{
    private const string ApiEndpoint = "https://api.groq.com/openai/v1/audio/transcriptions";
    private const string Model = "whisper-large-v3";
    private const int SampleRate = 16000;
    private const int MinAudioBytes = SampleRate * 2 / 4;

    private readonly HttpClient _httpClient;
    private readonly SettingsService _settings;
    private readonly ApiKeyManager _apiKeyManager;
    private bool _disposed;

    public SpeechRecognitionService(SettingsService settings, ApiKeyManager apiKeyManager)
    {
        _settings = settings;
        _apiKeyManager = apiKeyManager;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    public Task PreloadAsync()
    {
        return Task.CompletedTask;
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

        var apiKey = _apiKeyManager.GetKey();
        var language = _settings.Settings.Language;

        try
        {
            using var wavStream = ConvertPcmToWav(audioData);

            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(wavStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            content.Add(streamContent, "file", "audio.wav");
            content.Add(new StringContent(Model), "model");
            content.Add(new StringContent(language), "language");
            content.Add(new StringContent("0"), "temperature");

            using var request = new HttpRequestMessage(HttpMethod.Post, ApiEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"[Groq] API error {response.StatusCode}: {responseBody}");
                throw new Exception($"Groq API error: {response.StatusCode}");
            }

            _apiKeyManager.RecordUsage(apiKey);

            using var doc = JsonDocument.Parse(responseBody);
            var text = doc.RootElement.GetProperty("text").GetString();

            return text?.Trim() ?? string.Empty;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Groq] Transcription error: {ex.Message}");
            throw;
        }
    }

    public Task<string> FinishSessionAsync()
    {
        return Task.FromResult(string.Empty);
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

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _httpClient?.Dispose();
    }
}
