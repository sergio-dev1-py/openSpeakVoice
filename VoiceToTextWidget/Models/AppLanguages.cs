namespace VoiceToTextWidget.Models;

public static class AppLanguages
{
    public static readonly (string Code, string DisplayName)[] CommonLanguages = new[]
    {
        ("es", "Español"),
        ("en", "English"),
        ("pt", "Português"),
        ("fr", "Français"),
        ("de", "Deutsch"),
        ("it", "Italiano"),
        ("ja", "日本語"),
        ("ko", "한국어"),
        ("zh", "中文"),
        ("ru", "Русский"),
        ("ar", "العربية"),
        ("hi", "हिन्दी"),
        ("pl", "Polski"),
        ("tr", "Türkçe"),
        ("nl", "Nederlands"),
        ("sv", "Svenska"),
        ("da", "Dansk"),
        ("fi", "Suomi"),
        ("no", "Norsk"),
        ("cs", "Čeština"),
    };

    public static readonly (string Code, string DisplayName)[] AllLanguages = new[]
    {
        ("af", "Afrikaans"),
        ("am", "አማርኛ"),
        ("ar", "العربية"),
        ("as", "অসমীয়া"),
        ("az", "Azərbaycanca"),
        ("be", "Беларуская"),
        ("bg", "Български"),
        ("bn", "বাংলা"),
        ("br", "Português (BR)"),
        ("bs", "Bosanski"),
        ("ca", "Català"),
        ("cs", "Čeština"),
        ("cy", "Cymraeg"),
        ("da", "Dansk"),
        ("de", "Deutsch"),
        ("el", "Ελληνικά"),
        ("en", "English"),
        ("eo", "Esperanto"),
        ("es", "Español"),
        ("et", "Eesti"),
        ("eu", "Euskara"),
        ("fa", "فارسی"),
        ("fi", "Suomi"),
        ("fil", "Filipino"),
        ("fr", "Français"),
        ("gl", "Galego"),
        ("gu", "ગુજરાતી"),
        ("ha", "Hausa"),
        ("he", "עברית"),
        ("hi", "हिन्दी"),
        ("hr", "Hrvatski"),
        ("hu", "Magyar"),
        ("hy", "Հայերեն"),
        ("id", "Bahasa Indonesia"),
        ("is", "Íslenska"),
        ("it", "Italiano"),
        ("ja", "日本語"),
        ("jv", "Basa Jawa"),
        ("ka", "ქართული"),
        ("kk", "Қазақша"),
        ("km", "ខ្មែរ"),
        ("kn", "ಕನ್ನಡ"),
        ("ko", "한국어"),
        ("ku", "Kurdî"),
        ("ky", "Кыргызча"),
        ("la", "Latina"),
        ("lo", "ລາວ"),
        ("lt", "Lietuvių"),
        ("lv", "Latviešu"),
        ("mg", "Malagasy"),
        ("mk", "Македонски"),
        ("ml", "മലയാളം"),
        ("mn", "Монгол"),
        ("mr", "मराठी"),
        ("ms", "Bahasa Melayu"),
        ("mt", "Malti"),
        ("my", "မြန်မာ"),
        ("ne", "नेपाली"),
        ("nl", "Nederlands"),
        ("no", "Norsk"),
        ("om", "Afaan Oromoo"),
        ("pa", "ਪੰਜਾਬੀ"),
        ("pl", "Polski"),
        ("ps", "پښتو"),
        ("pt", "Português"),
        ("ro", "Română"),
        ("ru", "Русский"),
        ("sa", "संस्कृतम्"),
        ("sd", "سنڌي"),
        ("si", "සිංහල"),
        ("sk", "Slovenčina"),
        ("sl", "Slovenščina"),
        ("so", "Soomaali"),
        ("sq", "Shqip"),
        ("sr", "Српски"),
        ("su", "Basa Sunda"),
        ("sv", "Svenska"),
        ("sw", "Kiswahili"),
        ("ta", "தமிழ்"),
        ("te", "తెలుగు"),
        ("th", "ไทย"),
        ("tr", "Türkçe"),
        ("ug", "ئۇيغۇرچە"),
        ("uk", "Українська"),
        ("ur", "اردو"),
        ("uz", "Oʻzbekcha"),
        ("vi", "Tiếng Việt"),
        ("wo", "Wolof"),
        ("xh", "isiXhosa"),
        ("yi", "ייִדיש"),
        ("yo", "Yorùbá"),
        ("zh", "中文"),
        ("zu", "isiZulu"),
    };

    public static string GetDisplayName(string code)
    {
        foreach (var lang in AllLanguages)
        {
            if (lang.Code == code) return lang.DisplayName;
        }
        return code;
    }

    public static int FindCommonIndex(string code)
    {
        for (int i = 0; i < CommonLanguages.Length; i++)
        {
            if (CommonLanguages[i].Code == code) return i;
        }
        return -1;
    }

    public static int FindAllIndex(string code)
    {
        for (int i = 0; i < AllLanguages.Length; i++)
        {
            if (AllLanguages[i].Code == code) return i;
        }
        return -1;
    }
}
