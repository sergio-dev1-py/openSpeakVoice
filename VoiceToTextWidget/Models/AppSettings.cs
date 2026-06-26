using System.Text.Json.Serialization;

namespace VoiceToTextWidget.Models;

public class AppSettings
{
    public int HotKeyId { get; set; } = 1;
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Keys HotKey { get; set; } = Keys.F8;
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ModifierKeys Modifiers { get; set; } = ModifierKeys.None;
    
    public string ApiKey1 { get; set; } = string.Empty;
    public string ApiKey2 { get; set; } = string.Empty;
    
    public int ApiKey1RequestCount { get; set; } = 0;
    public int ApiKey2RequestCount { get; set; } = 0;
    
    public DateTime ApiKey1WindowStart { get; set; } = DateTime.MinValue;
    public DateTime ApiKey2WindowStart { get; set; } = DateTime.MinValue;
    
    public string Language { get; set; } = "es";

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SttProvider ActiveProvider { get; set; } = SttProvider.GroqApi;

    public string LocalModelName { get; set; } = "small";

    public bool UseGpuAcceleration { get; set; } = true;

    public int WidgetPosX { get; set; } = 100;
    public int WidgetPosY { get; set; } = 100;

    [JsonIgnore]
    public string LocalModelPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "VoiceToTextWidget", "Models", $"ggml-{LocalModelName}.bin");

    [JsonIgnore]
    public string ActiveApiKey => ApiKey1;

    [JsonIgnore]
    public bool HasAnyKey => !string.IsNullOrWhiteSpace(ApiKey1) || !string.IsNullOrWhiteSpace(ApiKey2);
}

[Flags]
public enum ModifierKeys : uint
{
    None = 0x0000,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Win = 0x0008
}
