//! Language tables — parity with the legacy C# `AppLanguages`.
//! 20 common languages (fast dropdown) + 93 "all languages" (searchable picker).

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub struct Language {
    pub code: &'static str,
    pub name: &'static str,
}

const fn l(code: &'static str, name: &'static str) -> Language {
    Language { code, name }
}

pub const COMMON_LANGUAGES: &[Language] = &[
    l("es", "Español"),
    l("en", "English"),
    l("pt", "Português"),
    l("fr", "Français"),
    l("de", "Deutsch"),
    l("it", "Italiano"),
    l("ja", "日本語"),
    l("ko", "한국어"),
    l("zh", "中文"),
    l("ru", "Русский"),
    l("ar", "العربية"),
    l("hi", "हिन्दी"),
    l("pl", "Polski"),
    l("tr", "Türkçe"),
    l("nl", "Nederlands"),
    l("sv", "Svenska"),
    l("da", "Dansk"),
    l("fi", "Suomi"),
    l("no", "Norsk"),
    l("cs", "Čeština"),
];

pub const ALL_LANGUAGES: &[Language] = &[
    l("es", "Español"),
    l("en", "English"),
    l("br", "Português (BR)"),
    l("pt", "Português"),
    l("fr", "Français"),
    l("de", "Deutsch"),
    l("it", "Italiano"),
    l("ja", "日本語"),
    l("ko", "한국어"),
    l("zh", "中文"),
    l("yue", "粵語 (Cantonés)"),
    l("ru", "Русский"),
    l("ar", "العربية"),
    l("hi", "हिन्दी"),
    l("pl", "Polski"),
    l("tr", "Türkçe"),
    l("nl", "Nederlands"),
    l("sv", "Svenska"),
    l("da", "Dansk"),
    l("fi", "Suomi"),
    l("no", "Norsk"),
    l("cs", "Čeština"),
    l("ca", "Català"),
    l("eu", "Euskara"),
    l("gl", "Galego"),
    l("af", "Afrikaans"),
    l("sq", "Shqip"),
    l("am", "አማርኛ"),
    l("hy", "Հայերեն"),
    l("az", "Azərbaycan"),
    l("be", "Беларуская"),
    l("bn", "বাংলা"),
    l("bs", "Bosanski"),
    l("bg", "Български"),
    l("my", "ဗမာ"),
    l("hr", "Hrvatski"),
    l("et", "Eesti"),
    l("fil", "Filipino"),
    l("ka", "ქართული"),
    l("el", "Ελληνικά"),
    l("gu", "ગુજરાતી"),
    l("ht", "Kreyòl Ayisyen"),
    l("ha", "Hausa"),
    l("haw", "ʻŌlelo Hawaiʻi"),
    l("he", "עברית"),
    l("hu", "Magyar"),
    l("is", "Íslenska"),
    l("id", "Bahasa Indonesia"),
    l("jv", "Basa Jawa"),
    l("kk", "Қазақ"),
    l("km", "ភាសាខ្មែរ"),
    l("kn", "ಕನ್ನಡ"),
    l("ky", "Кыргызча"),
    l("lo", "ລາວ"),
    l("la", "Latina"),
    l("lv", "Latviešu"),
    l("lt", "Lietuvių"),
    l("mk", "Македонски"),
    l("ms", "Bahasa Melayu"),
    l("ml", "മലയാളം"),
    l("mt", "Malti"),
    l("mi", "Te Reo Māori"),
    l("mr", "मराठी"),
    l("mn", "Монгол"),
    l("ne", "नेपाली"),
    l("fa", "فارسی"),
    l("pa", "ਪੰਜਾਬੀ"),
    l("ro", "Română"),
    l("sr", "Српски"),
    l("si", "සිංහල"),
    l("sk", "Slovenčina"),
    l("sl", "Slovenščina"),
    l("so", "Soomaali"),
    l("su", "Basa Sunda"),
    l("sw", "Kiswahili"),
    l("tg", "Тоҷикӣ"),
    l("ta", "தமிழ்"),
    l("tt", "Татарча"),
    l("te", "తెలుగు"),
    l("th", "ไทย"),
    l("ti", "ትግርኛ"),
    l("uk", "Українська"),
    l("ur", "اردو"),
    l("uz", "Oʻzbek"),
    l("vi", "Tiếng Việt"),
    l("cy", "Cymraeg"),
    l("yi", "ייִדיש"),
    l("fo", "Føroyskt"),
    l("ps", "پښتو"),
    l("sd", "سنڌي"),
    l("oc", "Occitan"),
    l("nn", "Nynorsk"),
];

/// Native display name for a code, or `None` if unknown (caller can fall
/// back to the code itself).
pub fn display_name(code: &str) -> Option<&'static str> {
    find(ALL_LANGUAGES, code).map(|lg| lg.name)
}

pub fn find(list: &'static [Language], code: &str) -> Option<&'static Language> {
    list.iter().find(|lg| lg.code.eq_ignore_ascii_case(code))
}

/// Map app language codes to whisper language tokens. "br" is a legacy
/// app-level code (Português BR) that whisper does not know; whisper has a
/// single Portuguese model.
pub fn whisper_language(code: &str) -> Option<&'static str> {
    if code.is_empty() || code.eq_ignore_ascii_case("auto") {
        return None;
    }
    if code.eq_ignore_ascii_case("br") {
        return Some("pt");
    }
    find(ALL_LANGUAGES, code).map(|lg| lg.code)
}
