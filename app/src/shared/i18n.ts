// UI localization — es/en, superset of the legacy WidgetStrings keys.
export type Dict = Record<string, string>;

const es: Dict = {
  // Pill / status
  status_listening: "Escuchando",
  status_transcribing: "Transcribiendo",
  status_ready: "Listo",
  status_translating: "Traduciendo",

  // Menu
  menu_config: "Ajustes",
  menu_apikeys: "API Keys…",
  menu_provider: "Proveedor",
  menu_groq: "Groq (nube)",
  menu_local: "Whisper (local)",
  menu_model: "Modelos…",
  menu_mode: "Modo",
  mode_transcription: "Transcripción",
  mode_translation: "Traducción",
  menu_target_lang: "Idioma destino",
  menu_hide: "Ocultar widget",
  menu_exit: "Salir",

  // Settings
  settings_title: "Ajustes",
  settings_general: "General",
  settings_language: "Idioma",
  settings_mode: "Modo",
  settings_target_lang: "Idioma de destino",
  settings_hotkey: "Atajo de teclado",
  settings_hotkey_hint: "Pulsa una combinación de teclas",
  settings_modifiers: "Modificadores",
  settings_appearance: "Apariencia",
  settings_border_color: "Color del borde",
  settings_opacity: "Opacidad",
  settings_multicolor: "Borde multicolor",
  settings_multicolor_desc: "Anima el borde con todos los colores",
  settings_capture: "Capturar",
  settings_capture_prompt: "Esperando tecla… (Esc para cancelar)",
  settings_invalid_key: "Selecciona una tecla válida (no solo modificadores).",
  settings_all_languages: "Todos los idiomas…",
  settings_search: "Buscar idioma…",
  settings_save: "Guardar",
  settings_cancel: "Cancelar",
  settings_common: "Idiomas frecuentes",

  // Keys window
  keys_title: "API Keys de Groq",
  keys_subtitle: "Consigue keys gratis en console.groq.com",
  keys_key1: "Key 1",
  keys_key2: "Key 2",
  keys_usage: "Uso",
  keys_not_set: "(sin configurar)",
  keys_info: "Se guardan automáticamente. Rotación automática al llegar a 19 req/min por key.",
  keys_save: "Guardar",
  keys_cancel: "Cerrar",

  // Models window
  models_title: "Modelos de Whisper",
  models_subtitle: "Modelos locales para reconocimiento sin conexión",
  models_gpu: "Aceleración por GPU",
  models_gpu_hint: "Se usa al cargar el modelo (requiere soporte del sistema)",
  models_download: "Descargar",
  models_delete: "Borrar",
  models_cancel: "Cancelar",
  models_downloaded: "Descargado",
  models_active: "Activo",
  models_set_active: "Usar este modelo",
  models_confirm_delete: "¿Borrar el modelo?",
  models_close: "Cerrar",

  // Toasts
  toast_error_title: "Error",
  toast_saved: "Guardado",
};

const en: Dict = {
  status_listening: "Listening",
  status_transcribing: "Transcribing",
  status_ready: "Ready",
  status_translating: "Translating",

  menu_config: "Settings",
  menu_apikeys: "API Keys…",
  menu_provider: "Provider",
  menu_groq: "Groq (cloud)",
  menu_local: "Whisper (local)",
  menu_model: "Models…",
  menu_mode: "Mode",
  mode_transcription: "Transcription",
  mode_translation: "Translation",
  menu_target_lang: "Target language",
  menu_hide: "Hide widget",
  menu_exit: "Quit",

  settings_title: "Settings",
  settings_general: "General",
  settings_language: "Language",
  settings_mode: "Mode",
  settings_target_lang: "Target language",
  settings_hotkey: "Keyboard shortcut",
  settings_hotkey_hint: "Press a key combination",
  settings_modifiers: "Modifiers",
  settings_appearance: "Appearance",
  settings_border_color: "Border color",
  settings_opacity: "Opacity",
  settings_multicolor: "Multicolor border",
  settings_multicolor_desc: "Animate the border through every color",
  settings_capture: "Capture",
  settings_capture_prompt: "Waiting for key… (Esc to cancel)",
  settings_invalid_key: "Pick a valid key (modifiers alone are not allowed).",
  settings_all_languages: "All languages…",
  settings_search: "Search language…",
  settings_save: "Save",
  settings_cancel: "Cancel",
  settings_common: "Frequent languages",

  keys_title: "Groq API Keys",
  keys_subtitle: "Get free keys at console.groq.com",
  keys_key1: "Key 1",
  keys_key2: "Key 2",
  keys_usage: "Usage",
  keys_not_set: "(not configured)",
  keys_info: "Saved automatically. Automatic rotation at 19 req/min per key.",
  keys_save: "Save",
  keys_cancel: "Close",

  models_title: "Whisper Models",
  models_subtitle: "Local models for offline recognition",
  models_gpu: "GPU acceleration",
  models_gpu_hint: "Applied when loading the model (requires system support)",
  models_download: "Download",
  models_delete: "Delete",
  models_cancel: "Cancel",
  models_downloaded: "Downloaded",
  models_active: "Active",
  models_set_active: "Use this model",
  models_confirm_delete: "Delete this model?",
  models_close: "Close",

  toast_error_title: "Error",
  toast_saved: "Saved",
};

const dicts: Record<string, Dict> = { es, en };

export function tr(lang: string, key: string): string {
  return dicts[lang]?.[key] ?? es[key] ?? key;
}

export function makeT(lang: string): (key: string) => string {
  return (key: string) => tr(lang, key);
}
