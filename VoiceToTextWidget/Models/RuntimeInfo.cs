namespace VoiceToTextWidget.Models;

public class RuntimeInfo
{
    public string RuntimeName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public bool IsGpuAvailable { get; set; }

    public string DisplayName => IsGpuAvailable
        ? $"GPU: {DeviceName} ({RuntimeName})"
        : $"CPU: {RuntimeName}";
}
