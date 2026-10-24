//! Global shortcut registration/re-registration driven by settings.

use crate::hotkey::shortcut_string;
use crate::settings::SttProvider;
use tauri::{AppHandle, Manager};
use tauri_plugin_global_shortcut::{GlobalShortcutExt, Shortcut};

pub fn reregister(app: &AppHandle) -> Result<(), String> {
    let shortcut_api = app.global_shortcut();
    let _ = shortcut_api.unregister_all();

    let settings = app.state::<crate::state::AppManager>().settings.get();
    let combo = shortcut_string(&settings.hot_key, settings.modifiers)
        .unwrap_or_else(|| "F8".to_string());

    let shortcut: Shortcut = combo
        .parse()
        .map_err(|e| format!("atajo inválido ({combo}): {e}"))?;

    shortcut_api
        .register(shortcut)
        .map_err(|e| format!("no se pudo registrar {combo}: {e}"))?;
    log::info!("hotkey registered: {combo} (provider {:?})", settings.active_provider);
    Ok(())
}

/// Convenience used by main to build the plugin handler closure.
pub fn hotkey_handler(app: &AppHandle, _shortcut: &Shortcut, event: tauri_plugin_global_shortcut::ShortcutEvent) {
    if event.state == tauri_plugin_global_shortcut::ShortcutState::Pressed {
        crate::state::on_hotkey(app);
    }
}

/// Re-export for menu label building.
pub fn provider_name(settings: &crate::settings::AppSettings) -> &'static str {
    match settings.active_provider {
        SttProvider::GroqApi => "Groq",
        SttProvider::LocalWhisper => "Whisper",
    }
}
