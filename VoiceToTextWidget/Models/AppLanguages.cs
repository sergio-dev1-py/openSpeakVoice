namespace VoiceToTextWidget.Models;

public static class AppLanguages
{
    public static readonly (string Code, string DisplayName)[] Languages = new[]
    {
        ("es", "Español"),
        ("en", "English"),
        ("fr", "Français"),
        ("de", "Deutsch"),
        ("pt", "Português"),
        ("it", "Italiano"),
        ("ja", "日本語"),
        ("ko", "한국어"),
        ("zh", "中文"),
        ("ru", "Русский"),
        ("ar", "العربية"),
        ("hi", "हिन्दी"),
    };

    public static readonly (string Code, string DisplayName)[] TargetLanguages = new[]
    {
        ("es", "Español"),
        ("en", "English"),
        ("fr", "Français"),
        ("de", "Deutsch"),
        ("pt", "Português"),
        ("it", "Italiano"),
        ("ja", "日本語"),
        ("ko", "한국어"),
        ("zh", "中文"),
        ("ru", "Русский"),
        ("ar", "العربية"),
        ("hi", "हिन्दी"),
    };

    public static string GetDisplayName(string code)
    {
        foreach (var lang in Languages)
        {
            if (lang.Code == code) return lang.DisplayName;
        }
        return code;
    }

    public static string GetTargetDisplayName(string code)
    {
        foreach (var lang in TargetLanguages)
        {
            if (lang.Code == code) return lang.DisplayName;
        }
        return code;
    }
}
