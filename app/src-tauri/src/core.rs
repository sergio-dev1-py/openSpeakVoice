//! Application logic shared by the Slint UI. This replaces the old Tauri
//! command/event layer: state is kept in atomics/mutexes and the UI polls it.

use crate::audio::{AudioHandle, MIN_SAMPLES_16K};
use crate::groq;
use crate::inject::inject_text;
use crate::languages;
use crate::settings::{AppSettings, ModifierKeys, SettingsService, SttProvider};
use crate::whisper::{self, DownloadGuard, ModelInfo, WhisperEngine};
use std::collections::{HashMap, HashSet, VecDeque};
use std::sync::atomic::{AtomicU8, Ordering};
use std::sync::{Arc, Mutex};
use std::time::{Duration, Instant};

pub const STATE_IDLE: u8 = 0;
pub const STATE_LISTENING: u8 = 1;
pub const STATE_TRANSCRIBING: u8 = 2;
pub const MAX_RECORDING: Duration = Duration::from_secs(300);

pub struct AppCore {
    pub settings: Arc<SettingsService>,
    pub audio: Arc<AudioHandle>,
    pub whisper: Arc<Mutex<WhisperEngine>>,
    pub state: Arc<AtomicU8>,
    download_guard: Arc<DownloadGuard>,
    /// Queued UI notifications: `(is_error, message)`.
    messages: Mutex<VecDeque<(bool, String)>>,
    downloading: Mutex<HashSet<String>>,
    progress: Mutex<HashMap<String, u32>>,
    recording_started: Mutex<Option<Instant>>,
    runtime: tokio::runtime::Runtime,
}

impl AppCore {
    pub fn new(settings: SettingsService) -> Arc<Self> {
        let runtime = tokio::runtime::Builder::new_multi_thread()
            .worker_threads(2)
            .thread_name("vtt-worker")
            .enable_all()
            .build()
            .expect("failed to create tokio runtime");
        Arc::new(Self {
            settings: Arc::new(settings),
            audio: Arc::new(AudioHandle::new()),
            whisper: Arc::new(Mutex::new(WhisperEngine::default())),
            state: Arc::new(AtomicU8::new(STATE_IDLE)),
            download_guard: Arc::new(DownloadGuard::default()),
            messages: Mutex::new(VecDeque::new()),
            downloading: Mutex::new(HashSet::new()),
            progress: Mutex::new(HashMap::new()),
            recording_started: Mutex::new(None),
            runtime,
        })
    }

    pub fn state(&self) -> u8 {
        self.state.load(Ordering::SeqCst)
    }

    pub fn is_listening(&self) -> bool {
        self.state() == STATE_LISTENING
    }

    pub fn error(&self, message: impl Into<String>) {
        let message = message.into();
        log::warn!("error: {message}");
        if let Ok(mut queue) = self.messages.lock() {
            queue.push_back((true, message));
        }
    }

    pub fn info(&self, message: impl Into<String>) {
        let message = message.into();
        log::info!("{message}");
        if let Ok(mut queue) = self.messages.lock() {
            queue.push_back((false, message));
        }
    }

    /// Pops the oldest UI notification, if any.
    pub fn take_message(&self) -> Option<(bool, String)> {
        self.messages.lock().ok()?.pop_front()
    }

    fn set_state(&self, value: u8) {
        self.state.store(value, Ordering::SeqCst);
    }

    pub fn toggle_recording(self: &Arc<Self>) {
        match self.state() {
            STATE_IDLE => {
                let settings = self.settings.get();
                if settings.active_provider == SttProvider::GroqApi && !settings.has_any_key() {
                    self.error(
                        "No hay API keys configuradas. Abre el menú → API Keys para configurarlas.",
                    );
                    return;
                }
                match self.audio.start() {
                    Ok(()) => {
                        log::info!(
                            "recording started (provider {:?})",
                            settings.active_provider
                        );
                        *self.recording_started.lock().unwrap() = Some(Instant::now());
                        self.set_state(STATE_LISTENING);
                    }
                    Err(e) => self.error(e),
                }
            }
            STATE_LISTENING => {
                log::info!("recording stopped, transcribing…");
                *self.recording_started.lock().unwrap() = None;
                self.set_state(STATE_TRANSCRIBING);
                let core = Arc::clone(self);
                self.runtime
                    .spawn(async move { core.transcribe_task().await });
            }
            _ => {}
        }
    }

    /// Stop automatically after the configured maximum to keep the PCM buffer
    /// bounded. Called from the UI timer.
    pub fn stop_if_too_long(self: &Arc<Self>) {
        if !self.is_listening() {
            return;
        }
        let too_long = self
            .recording_started
            .lock()
            .ok()
            .and_then(|started| *started)
            .map(|started| started.elapsed() >= MAX_RECORDING)
            .unwrap_or(false);
        if too_long {
            log::info!("maximum recording duration reached; stopping");
            self.toggle_recording();
        }
    }

    pub fn audio_level(&self) -> f32 {
        if self.is_listening() {
            self.audio.level()
        } else {
            0.0
        }
    }

    async fn transcribe_task(self: Arc<Self>) {
        let audio = Arc::clone(&self.audio);
        let samples = self
            .runtime
            .spawn_blocking(move || audio.stop())
            .await
            .unwrap_or_default();

        if samples.len() < MIN_SAMPLES_16K {
            self.error(
                "No se capturó audio del micrófono. Revisa que el micrófono correcto esté activo.",
            );
            self.set_state(STATE_IDLE);
            return;
        }

        let settings = self.settings.get();
        let result = self.run_stt(&settings, &samples).await;
        match result {
            Ok(text) => {
                if text.trim().is_empty() {
                    self.error("No se reconoció texto. Prueba hablando más claro o más fuerte.");
                } else {
                    let text_for_task = text.clone();
                    match self
                        .runtime
                        .spawn_blocking(move || inject_text(&text_for_task))
                        .await
                        .unwrap_or_else(|e| Err(format!("Error interno: {e}")))
                    {
                        Ok(()) => {
                            // Success is silent: the inserted text itself is the
                            // feedback. Only errors should open a toast.
                            log::info!("text injected");
                        }
                        Err(e) => self.error(e),
                    }
                }
            }
            Err(e) => self.error(e),
        }
        self.set_state(STATE_IDLE);
    }

    async fn run_stt(&self, settings: &AppSettings, samples: &[i16]) -> Result<String, String> {
        let language_hint = languages::whisper_language(&settings.app_language);
        match settings.active_provider {
            SttProvider::LocalWhisper => {
                let engine = Arc::clone(&self.whisper);
                let model = settings.local_model_name.clone();
                let lang = language_hint.map(str::to_string);
                let owned = samples.to_vec();
                self.runtime
                    .spawn_blocking(move || {
                        engine
                            .lock()
                            .unwrap()
                            .transcribe(&owned, &model, lang.as_deref())
                    })
                    .await
                    .map_err(|e| format!("Error interno: {e}"))?
            }
            SttProvider::GroqApi => {
                let key_choice = {
                    let snapshot = self.settings.get();
                    groq::pick_key(&snapshot)?
                };
                let wav = groq::pcm_to_wav(samples);
                let text = groq::transcribe(&key_choice.key, wav, language_hint).await?;
                groq::record_usage(&self.settings, &key_choice);
                if settings.mode == "translation" {
                    let target = settings.target_language.clone();
                    let translated = groq::translate(&key_choice.key, text.trim(), &target).await?;
                    groq::record_usage(&self.settings, &key_choice);
                    Ok(translated)
                } else {
                    Ok(text)
                }
            }
        }
    }

    // ——— settings mutations ———

    pub fn update_settings(&self, f: impl FnOnce(&mut AppSettings)) -> AppSettings {
        let settings = self.settings.update(f);
        settings
    }

    pub fn set_language(&self, code: String) {
        self.update_settings(|s| {
            s.language = code.clone();
            s.app_language = code;
        });
    }

    pub fn set_mode(&self, mode: String) {
        self.update_settings(|s| {
            if mode == "transcription" || mode == "translation" {
                s.mode = mode;
            }
        });
    }

    pub fn set_target_language(&self, code: String) {
        self.update_settings(|s| s.target_language = code);
    }

    pub fn set_provider(&self, provider: String) {
        self.update_settings(|s| {
            s.active_provider = if provider == "LocalWhisper" {
                SttProvider::LocalWhisper
            } else {
                SttProvider::GroqApi
            };
        });
    }

    pub fn set_appearance(&self, color: String, opacity: u8, multicolor: bool) {
        self.update_settings(|s| {
            s.border_color = color;
            s.border_opacity = opacity;
            s.multicolor_border = multicolor;
        });
    }

    pub fn set_theme(&self, theme: String) {
        self.update_settings(|s| {
            s.theme = if theme == "light" {
                "light".into()
            } else {
                "dark".into()
            }
        });
    }

    pub fn set_api_keys(&self, key1: String, key2: String) {
        self.update_settings(|s| {
            s.api_key1 = key1.trim().to_string();
            s.api_key2 = key2.trim().to_string();
        });
    }

    pub fn set_hotkey(&self, key: String, modifiers: ModifierKeys) {
        self.update_settings(|s| {
            s.hot_key = key.trim().to_uppercase();
            s.modifiers = modifiers;
        });
    }

    pub fn set_local_model(&self, name: String, use_gpu: bool) {
        self.whisper.lock().unwrap().reset();
        self.update_settings(|s| {
            if whisper::MODELS.iter().any(|(n, _, _)| *n == name) {
                s.local_model_name = name;
            }
            s.use_gpu_acceleration = use_gpu;
        });
    }

    pub fn usage(&self) -> (u32, u32, u32, u32, bool, bool) {
        let settings = self.settings.get();
        let (u1, t1, u2, t2) = groq::usage_status(&settings);
        (
            u1,
            t1,
            u2,
            t2,
            !settings.api_key1.is_empty(),
            !settings.api_key2.is_empty(),
        )
    }

    // ——— models ———

    pub fn models(&self) -> Vec<ModelInfo> {
        whisper::list_models()
    }

    pub fn model_progress(&self, name: &str) -> Option<u32> {
        self.progress.lock().ok()?.get(name).copied()
    }

    pub fn model_is_downloading(&self, name: &str) -> bool {
        self.downloading
            .lock()
            .map(|s| s.contains(name))
            .unwrap_or(false)
    }

    pub fn start_download(self: &Arc<Self>, name: String) {
        if !self.downloading.lock().unwrap().is_empty() {
            self.error("Ya hay una descarga de modelo en curso. Espera a que termine o cancélala.");
            return;
        }
        self.download_guard.0.store(false, Ordering::SeqCst);
        self.downloading.lock().unwrap().insert(name.clone());
        self.progress.lock().unwrap().insert(name.clone(), 0);
        let core = Arc::clone(self);
        self.runtime.spawn(async move {
            let guard = Arc::clone(&core.download_guard);
            let progress_core = Arc::clone(&core);
            let progress_name = name.clone();
            let result = whisper::download_model(guard, &name, move |_, _, percent| {
                if let Ok(mut map) = progress_core.progress.lock() {
                    map.insert(progress_name.clone(), percent);
                }
            })
            .await;
            core.downloading.lock().unwrap().remove(&name);
            core.progress.lock().unwrap().remove(&name);
            if let Err(e) = result {
                if e != "Descarga cancelada" {
                    core.error(e);
                }
            }
        });
    }

    pub fn cancel_download(&self) {
        self.download_guard.0.store(true, Ordering::SeqCst);
    }

    pub fn delete_model(&self, name: &str) {
        if let Err(e) = whisper::delete_model(name) {
            self.error(e);
        }
    }
}
