namespace VoiceToTextWidget.Services;

public static class WidgetStrings
{
    private static readonly Dictionary<string, Dictionary<string, string>> _strings = new()
    {
        ["es"] = new()
        {
            ["menu_config"] = "Configuración...",
            ["menu_apikeys"] = "Configurar API Keys...",
            ["menu_provider"] = "Proveedor de voz",
            ["menu_groq"] = "Groq API (Nube)",
            ["menu_local"] = "Whisper Local (GPU/CPU)",
            ["menu_model"] = "Configurar modelo local...",
            ["menu_mode"] = "Modo de trabajo",
            ["mode_transcription"] = "Transcripción",
            ["mode_translation"] = "Traducción",
            ["menu_target_lang"] = "Traducir al idioma",
            ["menu_hide"] = "Ocultar",
            ["menu_exit"] = "Salir",
            ["status_listening"] = "Escuchando",
            ["status_transcribing"] = "Transcribiendo",
            ["status_translating"] = "Traduciendo",
            ["status_ready"] = "Inactivo",
            ["settings_language"] = "Idioma de la aplicación",
            ["settings_mode"] = "Modo de trabajo",
            ["settings_target_lang"] = "Traducir al idioma",
            ["settings_save"] = "Guardar",
            ["settings_cancel"] = "Cancelar",
            ["settings_hotkey"] = "Atajo principal:",
            ["settings_modifiers"] = "Modificadores opcionales:",
            ["settings_appearance"] = "Apariencia del widget",
            ["settings_border_color"] = "Color del borde:",
            ["settings_multicolor"] = "Multicolor (borde cambia con el estado del widget)",
            ["settings_multicolor_desc"] = "Multicolor (cambia con el estado del widget)",
            ["settings_capture"] = "Capturar",
            ["settings_capture_prompt"] = "Presiona una tecla...",
            ["settings_invalid_key"] = "Selecciona una tecla principal valida antes de guardar.",
            ["settings_all_languages"] = "Ver todos los idiomas...",
        },
        ["en"] = new()
        {
            ["menu_config"] = "Settings...",
            ["menu_apikeys"] = "Configure API Keys...",
            ["menu_provider"] = "Voice Provider",
            ["menu_groq"] = "Groq API (Cloud)",
            ["menu_local"] = "Whisper Local (GPU/CPU)",
            ["menu_model"] = "Configure local model...",
            ["menu_mode"] = "Work Mode",
            ["mode_transcription"] = "Transcription",
            ["mode_translation"] = "Translation",
            ["menu_target_lang"] = "Translate to language",
            ["menu_hide"] = "Hide",
            ["menu_exit"] = "Exit",
            ["status_listening"] = "Listening",
            ["status_transcribing"] = "Transcribing",
            ["status_translating"] = "Translating",
            ["status_ready"] = "Inactive",
            ["settings_language"] = "Application language",
            ["settings_mode"] = "Work mode",
            ["settings_target_lang"] = "Translate to language",
            ["settings_save"] = "Save",
            ["settings_cancel"] = "Cancel",
            ["settings_hotkey"] = "Main hotkey:",
            ["settings_modifiers"] = "Optional modifiers:",
            ["settings_appearance"] = "Widget appearance",
            ["settings_border_color"] = "Border color:",
            ["settings_multicolor"] = "Multicolor (border changes with widget state)",
            ["settings_multicolor_desc"] = "Multicolor (changes with widget state)",
            ["settings_capture"] = "Capture",
            ["settings_capture_prompt"] = "Press a key...",
            ["settings_invalid_key"] = "Please select a valid main key before saving.",
            ["settings_all_languages"] = "See all languages...",
        },
    };

    public static string Get(string key)
    {
        var lang = Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName;
        return Get(key, lang);
    }

    public static string Get(string key, string lang)
    {
        if (_strings.TryGetValue(lang, out var dict) && dict.TryGetValue(key, out var val))
            return val;
        if (_strings.TryGetValue("es", out var esDict) && esDict.TryGetValue(key, out var esVal))
            return esVal;
        return key;
    }
}
