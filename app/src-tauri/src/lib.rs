pub mod audio;
pub mod groq;
pub mod hotkey;
pub mod hotkey_reg;
pub mod i18n;
pub mod inject;
pub mod languages;
pub mod settings;
pub mod state;
pub mod tray;
pub mod whisper;

use state::AppManager;
use std::sync::atomic::Ordering;
use std::sync::{Arc, Mutex};
use tauri::{Emitter, Listener, Manager};

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    let settings_service = Arc::new(
        settings::SettingsService::new()
            .unwrap_or_else(|e| panic!("No se pudo inicializar la configuración: {e}")),
    );
    let initial = settings_service.get();

    tauri::Builder::default()
        .plugin(
            tauri_plugin_global_shortcut::Builder::new()
                .with_handler(|app, shortcut, event| {
                    hotkey_reg::hotkey_handler(app, shortcut, event);
                })
                .build(),
        )
        .manage(AppManager {
            settings: Arc::clone(&settings_service),
            audio: Arc::new(audio::AudioHandle::new()),
            whisper: Arc::new(Mutex::new(whisper::WhisperEngine::default())),
            state: Arc::new(std::sync::atomic::AtomicU8::new(state::STATE_IDLE)),
            download_guard: Arc::new(whisper::DownloadGuard::default()),
        })
        .setup(move |app| {
            // Restore pill position (stored/restored in physical px).
            if let Some(pill) = app.get_webview_window("pill") {
                use tauri::PhysicalPosition;
                let _ = pill.set_position(PhysicalPosition::new(
                    initial.widget_pos_x,
                    initial.widget_pos_y,
                ));
            }

            tray::setup(app.handle())?;

            // Errors surface as toasts anchored to the pill.
            let handle = app.handle().clone();
            app.listen("error", move |event| {
                if let Ok(payload) = serde_json::from_str::<serde_json::Value>(event.payload()) {
                    let msg = payload["message"].as_str().unwrap_or("Error").to_string();
                    state::show_toast(handle.clone(), "error".into(), msg);
                }
            });
            let handle = app.handle().clone();
            app.listen("result", move |_event| {
                let lang = handle
                    .try_state::<AppManager>()
                    .map(|m| m.settings.get().app_language)
                    .unwrap_or_else(|| "es".to_string());
                let msg = if lang == "en" { "Text inserted" } else { "Texto insertado" };
                state::show_toast(handle.clone(), "success".into(), msg.into());
            });

            hotkey_reg::reregister(app.handle())
                .unwrap_or_else(|e| log::warn!("hotkey registration failed: {e}"));

            // Audio level → pill waveform (60 ms cadence while listening).
            let handle = app.handle().clone();
            tauri::async_runtime::spawn(async move {
                let mut interval = tokio::time::interval(std::time::Duration::from_millis(60));
                loop {
                    interval.tick().await;
                    let mgr = handle.state::<AppManager>();
                    if mgr.state.load(Ordering::SeqCst) == state::STATE_LISTENING {
                        let level = mgr.audio.level();
                        let _ = handle.emit("audio-level", serde_json::json!({ "level": level }));
                    }
                }
            });

            Ok(())
        })
        .on_window_event(|window, event| {
            // The popup menu closes itself when it loses focus.
            if window.label() == "menu" {
                if let tauri::WindowEvent::Focused(false) = event {
                    let _ = window.hide();
                }
            }
        })
        .invoke_handler(tauri::generate_handler![
            state::get_state,
            state::toggle_recording,
            state::get_settings,
            state::get_languages,
            state::set_language,
            state::set_mode,
            state::set_target_language,
            state::set_provider,
            state::set_api_keys,
            state::set_appearance,
            state::set_local_model,
            state::set_hotkey,
            state::set_widget_position,
            state::pill_move,
            state::open_menu,
            state::close_menu,
            state::resize_menu,
            state::show_toast,
            state::hide_pill,
            state::show_pill,
            state::show_window,
            state::hide_window,
            state::quit_app,
            state::get_models,
            state::start_download,
            state::cancel_download,
            state::delete_model,
            state::get_usage,
        ])
        .run(tauri::generate_context!())
        .expect("error while running VoiceToText");
}
