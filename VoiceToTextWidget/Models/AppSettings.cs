using System.Text.Json.Serialization;

namespace VoiceToTextWidget.Models;

public class AppSettings
{
    public int HotKeyId { get; set; } = 1;
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Keys HotKey { get; set; } = Keys.F8;
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ModifierKeys Modifiers { get; set; } = ModifierKeys.None;
    
    public string ApiKey { get; set; } = string.Empty;
    public string Language { get; set; } = "es";
    
    public int WidgetPosX { get; set; } = 100;
    public int WidgetPosY { get; set; } = 100;
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
