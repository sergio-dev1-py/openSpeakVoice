using System.Text.Json;
using VoiceToTextWidget.Models;

namespace VoiceToTextWidget.Services;

public sealed class SettingsService
{
    private readonly string _settingsPath;
    private AppSettings _settings = null!;
    
    public AppSettings Settings => _settings;
    
    public SettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appData, "VoiceToTextWidget");
        Directory.CreateDirectory(appFolder);
        _settingsPath = Path.Combine(appFolder, "settings.json");
        Load();
    }
    
    public void Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                _settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            else
            {
                _settings = new AppSettings();
            }
        }
        catch
        {
            _settings = new AppSettings();
        }
    }
    
    public void Save()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_settings, options);
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
        }
    }
    
    public void UpdateHotKey(Keys key, ModifierKeys modifiers)
    {
        _settings.HotKey = key;
        _settings.Modifiers = modifiers;
        Save();
    }
    
    public void UpdateWidgetPosition(int x, int y)
    {
        _settings.WidgetPosX = x;
        _settings.WidgetPosY = y;
        Save();
    }

    public void UpdateApiKey1(string apiKey)
    {
        _settings.ApiKey1 = apiKey;
        Save();
    }

    public void UpdateApiKey2(string apiKey)
    {
        _settings.ApiKey2 = apiKey;
        Save();
    }

    public void UpdateProvider(SttProvider provider)
    {
        _settings.ActiveProvider = provider;
        Save();
    }

    public void UpdateLocalModel(string modelName, bool useGpu)
    {
        _settings.LocalModelName = modelName;
        _settings.UseGpuAcceleration = useGpu;
        Save();
    }

    public void UpdateWhisperPrompt(string prompt)
    {
        _settings.WhisperPrompt = prompt;
        Save();
    }

    public void UpdateAppearance(string borderColor, bool multicolor)
    {
        _settings.BorderColor = borderColor;
        _settings.MulticolorBorder = multicolor;
        Save();
    }
}
