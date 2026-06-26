using VoiceToTextWidget.Models;

namespace VoiceToTextWidget.Services;

public sealed class ApiKeyManager
{
    private const int MaxRequestsPerMinute = 19;
    private static readonly TimeSpan WindowDuration = TimeSpan.FromMinutes(1);

    private readonly SettingsService _settings;

    public ApiKeyManager(SettingsService settings)
    {
        _settings = settings;
    }

    public string GetKey()
    {
        var s = _settings.Settings;

        ResetWindow1IfNeeded(s);
        ResetWindow2IfNeeded(s);

        var hasKey1 = !string.IsNullOrWhiteSpace(s.ApiKey1);
        var hasKey2 = !string.IsNullOrWhiteSpace(s.ApiKey2);

        if (!hasKey1 && !hasKey2)
            throw new InvalidOperationException("No API keys configured. Right-click the widget and select 'Configure API Keys'.");

        if (!hasKey2)
        {
            if (s.ApiKey1RequestCount >= MaxRequestsPerMinute)
                throw new InvalidOperationException($"API Key 1 rate limit reached ({MaxRequestsPerMinute}/min). Wait ~60s for reset.");
            return s.ApiKey1;
        }

        if (!hasKey1)
        {
            if (s.ApiKey2RequestCount >= MaxRequestsPerMinute)
                throw new InvalidOperationException($"API Key 2 rate limit reached ({MaxRequestsPerMinute}/min). Wait ~60s for reset.");
            return s.ApiKey2;
        }

        if (s.ApiKey1RequestCount < MaxRequestsPerMinute && s.ApiKey2RequestCount >= MaxRequestsPerMinute)
            return s.ApiKey1;

        if (s.ApiKey2RequestCount < MaxRequestsPerMinute && s.ApiKey1RequestCount >= MaxRequestsPerMinute)
            return s.ApiKey2;

        if (s.ApiKey1RequestCount <= s.ApiKey2RequestCount)
            return s.ApiKey1;

        return s.ApiKey2;
    }

    public void RecordUsage(string apiKey)
    {
        var s = _settings.Settings;

        if (apiKey == s.ApiKey1)
        {
            if (s.ApiKey1WindowStart == DateTime.MinValue)
                s.ApiKey1WindowStart = DateTime.UtcNow;
            s.ApiKey1RequestCount++;
        }
        else if (apiKey == s.ApiKey2)
        {
            if (s.ApiKey2WindowStart == DateTime.MinValue)
                s.ApiKey2WindowStart = DateTime.UtcNow;
            s.ApiKey2RequestCount++;
        }

        _settings.Save();
    }

    public (int used1, int total1, int used2, int total2) GetUsageStatus()
    {
        var s = _settings.Settings;

        ResetWindow1IfNeeded(s);
        ResetWindow2IfNeeded(s);

        return (s.ApiKey1RequestCount, MaxRequestsPerMinute, s.ApiKey2RequestCount, MaxRequestsPerMinute);
    }

    private static void ResetWindow1IfNeeded(AppSettings s)
    {
        if (s.ApiKey1WindowStart == DateTime.MinValue)
            return;

        if (DateTime.UtcNow - s.ApiKey1WindowStart >= WindowDuration)
        {
            s.ApiKey1RequestCount = 0;
            s.ApiKey1WindowStart = DateTime.MinValue;
        }
    }

    private static void ResetWindow2IfNeeded(AppSettings s)
    {
        if (s.ApiKey2WindowStart == DateTime.MinValue)
            return;

        if (DateTime.UtcNow - s.ApiKey2WindowStart >= WindowDuration)
        {
            s.ApiKey2RequestCount = 0;
            s.ApiKey2WindowStart = DateTime.MinValue;
        }
    }
}
