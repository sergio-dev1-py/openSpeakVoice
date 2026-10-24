//! Central state machine and Tauri command layer.
//! Idle → Listening → Transcribing → Idle (parity with the legacy widget),
//! plus all commands the frontend windows invoke.

use crate::audio::{AudioHandle, MIN_SAMPLES_16K};
use crate::groq;
use crate::inject::inject_text;
use crate::languages;
use crate::settings::{AppSettings, SettingsService, SttProvider};
use crate::whisper::{self, DownloadGuard, WhisperEngine};
use serde_json::json;
use std::sync::atomic::{AtomicU8, Ordering};
use std::sync::{Arc, Mutex};
use tauri::{AppHandle, Emitter, Manager, State};

pub const STATE_IDLE: u8 = 0;
pub const STATE_LISTENING: u8 = 1;
pub const STATE_TRANSCRIBING: u8 = 2;

pub struct AppManager {
    pub settings: Arc<SettingsService>,
    pub audio: Arc<AudioHandle>,
    pub whisper: Arc<Mutex<WhisperEngine>>,
    pub state: Arc<AtomicU8>,
    pub download_guard: Arc<DownloadGuard>,
}

fn state_name(v: u8) -> &'static str {
    match v {
        STATE_LISTENING => "listening",
        STATE_TRANSCRIBING => "transcribing",
        _ => "idle",
    }
}

pub fn set_state(app: &AppHandle, new_state: u8) {
    let mgr: State<AppManager> = app.state();
    mgr.state.store(new_state, Ordering::SeqCst);
    let _ = app.emit("state-changed", json!({ "state": state_name(new_state) }));
}

pub fn emit_error(app: &AppHandle, message: &str) {
    log::warn!("error: {message}");
    let _ = app.emit("error", json!({ "message": message }));
}

/// Hotkey handler: show the pill if hidden, otherwise toggle recording.
pub fn on_hotkey(app: &AppHandle) {
    if let Some(pill) = app.get_webview_window("pill") {
        if !pill.is_visible().unwrap_or(true) {
            let _ = pill.show();
            let _ = pill.set_focus();
            return;
        }
    }
    toggle_recording_impl(app);
}

pub fn toggle_recording_impl(app: &AppHandle) {
    let mgr: State<AppManager> = app.state();
    match mgr.state.load(Ordering::SeqCst) {
        STATE_IDLE => {
            let settings = mgr.settings.get();
            if settings.active_provider == SttProvider::GroqApi && !settings.has_any_key() {
                emit_error(
                    app,
                    "No hay API keys configuradas. Abre el menú → API Keys para configurarlas.",
                );
                return;
            }
            if let Err(e) = mgr.audio.start() {
                emit_error(app, &e);
                return;
            }
            set_state(app, STATE_LISTENING);
        }
        STATE_LISTENING => {
            set_state(app, STATE_TRANSCRIBING);
            let app = app.clone();
            tauri::async_runtime::spawn(async move { transcribe_task(app).await });
        }
        _ => {}
    }
}

async fn transcribe_task(app: AppHandle) {
    let mgr = app.state::<AppManager>();

    // AudioHandle is Send; stopping blocks briefly on the audio worker.
    let audio = Arc::clone(&mgr.audio);
    let samples = tauri::async_runtime::spawn_blocking(move || audio.stop())
        .await
        .unwrap_or_default();

    if samples.len() < MIN_SAMPLES_16K {
        emit_error(
            &app,
            "No se capturó audio del micrófono. Revisa que el micrófono correcto esté activo.",
        );
        set_state(&app, STATE_IDLE);
        return;
    }

    let settings = mgr.settings.get();
    let result = run_stt(&app, &mgr, &settings, &samples).await;

    match result {
        Ok(text) => {
            if text.trim().is_empty() {
                emit_error(
                    &app,
                    "No se reconoció texto. Prueba hablando más claro o más fuerte.",
                );
            } else {
                let injection = tauri::async_runtime::spawn_blocking(move || inject_text(&text))
                    .await
                    .unwrap_or_else(|e| Err(format!("Error interno: {e}")));
                match injection {
                    Ok(()) => {
                        let _ = app.emit("result", json!({ "ok": true }));
                    }
                    Err(e) => emit_error(&app, &e),
                }
            }
        }
        Err(e) => emit_error(&app, &e),
    }

    set_state(&app, STATE_IDLE);
}

async fn run_stt(
    app: &AppHandle,
    mgr: &State<'_, AppManager>,
    settings: &AppSettings,
    samples: &[i16],
) -> Result<String, String> {
    let language_hint = languages::whisper_language(&settings.app_language);

    match settings.active_provider {
        SttProvider::LocalWhisper => {
            let engine = Arc::clone(&mgr.whisper);
            let model = settings.local_model_name.clone();
            let lang = language_hint.map(str::to_string);
            let samples_owned = samples.to_vec();
            tauri::async_runtime::spawn_blocking(move || {
                engine
                    .lock()
                    .unwrap()
                    .transcribe(&samples_owned, &model, lang.as_deref())
            })
            .await
            .map_err(|e| format!("Error interno: {e}"))?
        }
        SttProvider::GroqApi => {
            let key_choice = {
                let snapshot = mgr.settings.get();
                groq::pick_key(&snapshot)?
            };
            let wav = groq::pcm_to_wav(samples);
            let text = groq::transcribe(&key_choice.key, wav, language_hint).await?;
            groq::record_usage(&mgr.settings, &key_choice);

            if settings.mode == "translation" {
                let target = settings.target_language.clone();
                let translated =
                    groq::translate(&key_choice.key, text.trim(), &target).await?;
                groq::record_usage(&mgr.settings, &key_choice);
                let _ = app.emit("status", json!({ "status": "translating" }));
                Ok(translated)
            } else {
                Ok(text)
            }
        }
    }
}

// ---------------------------------------------------------------------------
// Tauri commands
// ---------------------------------------------------------------------------

#[tauri::command]
pub fn get_state(mgr: State<'_, AppManager>) -> String {
    state_name(mgr.state.load(Ordering::SeqCst)).to_string()
}

#[tauri::command]
pub fn toggle_recording(app: AppHandle) {
    toggle_recording_impl(&app);
}

#[tauri::command]
pub fn get_settings(mgr: State<'_, AppManager>) -> AppSettings {
    mgr.settings.get()
}

#[tauri::command]
pub fn get_languages() -> serde_json::Value {
    let to_json = |list: &'static [languages::Language]| -> serde_json::Value {
        serde_json::to_value(
            list.iter()
                .map(|lg| json!({ "code": lg.code, "name": lg.name }))
                .collect::<Vec<_>>(),
        )
        .unwrap()
    };
    json!({ "common": to_json(languages::COMMON_LANGUAGES), "all": to_json(languages::ALL_LANGUAGES) })
}

#[tauri::command]
pub fn set_language(app: AppHandle, code: String) -> AppSettings {
    apply(app, |s| s.app_language = code)
}

#[tauri::command]
pub fn set_mode(app: AppHandle, mode: String) -> AppSettings {
    apply(app, |s| {
        if mode == "transcription" || mode == "translation" {
            s.mode = mode;
        }
    })
}

#[tauri::command]
pub fn set_target_language(app: AppHandle, code: String) -> AppSettings {
    apply(app, |s| s.target_language = code)
}

#[tauri::command]
pub fn set_provider(app: AppHandle, provider: String) -> AppSettings {
    let parsed = if provider == "LocalWhisper" {
        SttProvider::LocalWhisper
    } else {
        SttProvider::GroqApi
    };
    apply(app, |s| s.active_provider = parsed)
}

#[tauri::command]
pub fn set_api_keys(app: AppHandle, key1: String, key2: String) -> AppSettings {
    apply(app, |s| {
        s.api_key1 = key1.trim().to_string();
        s.api_key2 = key2.trim().to_string();
    })
}

#[tauri::command]
pub fn set_appearance(app: AppHandle, color: String, opacity: u8, multicolor: bool) -> AppSettings {
    apply(app, |s| {
        s.border_color = color;
        s.border_opacity = opacity;
        s.multicolor_border = multicolor;
    })
}

#[tauri::command]
pub fn set_local_model(app: AppHandle, name: String, use_gpu: bool) -> AppSettings {
    let mgr: State<AppManager> = app.state();
    mgr.whisper.lock().unwrap().reset();
    apply(app, |s| {
        if whisper::MODELS.iter().any(|(n, _, _)| *n == name) {
            s.local_model_name = name;
        }
        s.use_gpu_acceleration = use_gpu;
    })
}

#[tauri::command]
pub fn set_hotkey(app: AppHandle, key: String, modifiers: String) -> AppSettings {
    let settings = apply(app.clone(), |s| {
        s.hot_key = key.trim().to_uppercase();
        s.modifiers = crate::settings::ModifierKeys::parse(&modifiers);
    });
    if let Err(e) = crate::hotkey_reg::reregister(&app) {
        emit_error(&app, &format!("No se pudo registrar el nuevo atajo: {e}"));
    }
    settings
}

#[tauri::command]
pub fn set_widget_position(app: AppHandle, x: i32, y: i32) {
    let mgr: State<AppManager> = app.state();
    mgr.settings.update(|s| {
        s.widget_pos_x = x;
        s.widget_pos_y = y;
    });
}

/// Live pill move during drag (no settings write). `x`/`y` are physical px.
#[tauri::command]
pub fn pill_move(app: AppHandle, x: i32, y: i32) {
    if let Some(pill) = app.get_webview_window("pill") {
        use tauri::PhysicalPosition;
        let _ = pill.set_position(PhysicalPosition::new(x, y));
    }
}

/// Opens the popup menu anchored to the pill (below it, or above when there
/// is not enough room on screen).
#[tauri::command]
pub fn open_menu(app: AppHandle) {
    let Some(pill) = app.get_webview_window("pill") else {
        return;
    };
    let Some(menu) = app.get_webview_window("menu") else {
        return;
    };
    let Ok(pos) = pill.outer_position() else { return };
    let Ok(pill_size) = pill.outer_size() else { return };
    let scale = pill.scale_factor().unwrap_or(1.0);

    let menu_size = menu.outer_size().unwrap_or(tauri::PhysicalSize::new(
        (260.0 * scale) as u32,
        (520.0 * scale) as u32,
    ));
    let gap = (6.0 * scale) as i32;

    let mut y = pos.y + pill_size.height as i32 + gap;
    let bottom = y + menu_size.height as i32;
    if let Ok(Some(monitor)) = pill.current_monitor() {
        let screen_bottom = monitor.position().y + monitor.size().height as i32;
        if bottom > screen_bottom - 8 {
            y = pos.y - menu_size.height as i32 - gap;
        }
        let mut x = pos.x;
        if x + menu_size.width as i32 > monitor.position().x + monitor.size().width as i32 - 8 {
            x = monitor.position().x + monitor.size().width as i32 - menu_size.width as i32 - 8;
        }
        use tauri::PhysicalPosition;
        let _ = menu.set_position(PhysicalPosition::new(x, y));
    } else {
        use tauri::PhysicalPosition;
        let _ = menu.set_position(PhysicalPosition::new(pos.x, y));
    }

    let _ = menu.show();
    let _ = menu.set_focus();

    let mgr: State<AppManager> = app.state();
    let settings = mgr.settings.get();
    let (used1, total1, used2, total2) = groq::usage_status(&settings);
    let _ = app.emit(
        "menu-data",
        json!({
            "settings": serde_json::to_value(&settings).unwrap_or_default(),
            "usage": {
                "used1": used1, "total1": total1,
                "used2": used2, "total2": total2,
                "hasKey1": !settings.api_key1.is_empty(),
                "hasKey2": !settings.api_key2.is_empty(),
            }
        }),
    );
}

#[tauri::command]
pub fn close_menu(app: AppHandle) {
    if let Some(menu) = app.get_webview_window("menu") {
        let _ = menu.hide();
    }
}

/// The menu resizes itself to its content height (logical px).
#[tauri::command]
pub fn resize_menu(app: AppHandle, height: f64) {
    if let Some(menu) = app.get_webview_window("menu") {
        let _ = menu.set_size(tauri::LogicalSize::new(260.0, height.clamp(120.0, 620.0)));
        // Re-anchor so it never drifts off the pill.
        let _ = menu.emit("menu-refresh", json!({}));
    }
}

/// Shows a toast anchored near the pill: kind = "error" | "success".
#[tauri::command]
pub fn show_toast(app: AppHandle, kind: String, message: String) {
    let Some(pill) = app.get_webview_window("pill") else { return };
    let Some(toast) = app.get_webview_window("toast") else { return };
    let Ok(pos) = pill.outer_position() else { return };
    let Ok(pill_size) = pill.outer_size() else { return };
    let Ok(toast_size) = toast.outer_size() else { return };
    let scale = pill.scale_factor().unwrap_or(1.0);
    let gap = (8.0 * scale) as i32;

    let mut y = pos.y + pill_size.height as i32 + gap;
    if let Ok(Some(monitor)) = pill.current_monitor() {
        let screen_bottom = monitor.position().y + monitor.size().height as i32;
        if y + toast_size.height as i32 > screen_bottom - 8 {
            y = pos.y - toast_size.height as i32 - gap;
        }
    }
    let x = pos.x + (pill_size.width as i32 - toast_size.width as i32) / 2;
    use tauri::PhysicalPosition;
    let _ = toast.set_position(PhysicalPosition::new(x, y));
    let _ = toast.show();
    let _ = app.emit("toast-data", json!({ "kind": kind, "message": message }));
}

#[tauri::command]
pub fn hide_pill(app: AppHandle) {
    if let Some(pill) = app.get_webview_window("pill") {
        let _ = pill.hide();
    }
    close_menu(app.clone());
}

#[tauri::command]
pub fn show_pill(app: AppHandle) {
    if let Some(pill) = app.get_webview_window("pill") {
        let _ = pill.show();
        let _ = pill.set_focus();
    }
}

#[tauri::command]
pub fn show_window(app: AppHandle, label: String) {
    close_menu(app.clone());
    if let Some(win) = app.get_webview_window(&label) {
        let _ = win.show();
        let _ = win.center();
        let _ = win.set_focus();
    }
}

#[tauri::command]
pub fn hide_window(app: AppHandle, label: String) {
    if let Some(win) = app.get_webview_window(&label) {
        let _ = win.hide();
    }
}

#[tauri::command]
pub fn quit_app(app: AppHandle) {
    app.exit(0);
}

#[tauri::command]
pub fn get_models() -> Vec<whisper::ModelInfo> {
    whisper::list_models()
}

#[tauri::command]
pub fn start_download(app: AppHandle, name: String) {
    let mgr: State<AppManager> = app.state();
    let guard = Arc::clone(&mgr.download_guard);
    let app2 = app.clone();
    tauri::async_runtime::spawn(async move {
        if let Err(e) = whisper::download_model(app2.clone(), guard, &name).await {
            let _ = app2.emit("model-error", json!({ "name": name, "message": e }));
        }
    });
}

#[tauri::command]
pub fn cancel_download(app: AppHandle) {
    let mgr: State<AppManager> = app.state();
    mgr.download_guard.0.store(true, Ordering::SeqCst);
}

#[tauri::command]
pub fn delete_model(app: AppHandle, name: String) -> Result<(), String> {
    whisper::delete_model(&name)?;
    let _ = app.emit("models-changed", json!({}));
    Ok(())
}

#[tauri::command]
pub fn get_usage(mgr: State<'_, AppManager>) -> serde_json::Value {
    let settings = mgr.settings.get();
    let (used1, total1, used2, total2) = groq::usage_status(&settings);
    json!({
        "used1": used1, "total1": total1,
        "used2": used2, "total2": total2,
        "hasKey1": !settings.api_key1.is_empty(),
        "hasKey2": !settings.api_key2.is_empty(),
    })
}

/// Applies a settings mutation, saves, and notifies the frontend.
fn apply(app: AppHandle, f: impl FnOnce(&mut AppSettings)) -> AppSettings {
    let mgr: State<AppManager> = app.state();
    let settings = mgr.settings.update(f);
    let _ = app.emit(
        "settings-changed",
        json!({ "settings": serde_json::to_value(&settings).unwrap_or_default() }),
    );
    settings
}
