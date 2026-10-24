//! Settings persistence — 100% compatible with the legacy C# settings.json
//! (same PascalCase keys, same %LOCALAPPDATA%\VoiceToTextWidget\ folder on
//! Windows so existing keys and downloaded models are reused).

use serde::{Deserialize, Serialize};
use std::path::PathBuf;
use std::sync::RwLock;

pub const APP_DIR_NAME: &str = "VoiceToTextWidget";

#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize, Default)]
pub enum SttProvider {
    #[default]
    #[serde(rename = "GroqApi")]
    GroqApi,
    #[serde(rename = "LocalWhisper")]
    LocalWhisper,
}

/// Bit flags matching the C# `ModifierKeys` enum. Serialized as the enum's
/// `ToString()` output, e.g. "None", "Control", "Control, Alt", "Control, Shift, Win".
#[derive(Debug, Clone, Copy, PartialEq, Eq, Default)]
pub struct ModifierKeys(pub u8);

impl ModifierKeys {
    pub const NONE: ModifierKeys = ModifierKeys(0);
    pub const ALT: ModifierKeys = ModifierKeys(1);
    pub const CONTROL: ModifierKeys = ModifierKeys(2);
    pub const SHIFT: ModifierKeys = ModifierKeys(4);
    pub const WIN: ModifierKeys = ModifierKeys(8);

    pub fn contains(self, other: ModifierKeys) -> bool {
        self.0 & other.0 == other.0
    }

    pub fn is_empty(self) -> bool {
        self.0 == 0
    }

    pub fn parse(s: &str) -> ModifierKeys {
        let mut flags = 0u8;
        for part in s.split(',') {
            match part.trim().to_ascii_lowercase().as_str() {
                "alt" => flags |= Self::ALT.0,
                "control" => flags |= Self::CONTROL.0,
                "shift" => flags |= Self::SHIFT.0,
                "win" => flags |= Self::WIN.0,
                _ => {}
            }
        }
        ModifierKeys(flags)
    }

    pub fn to_csharp_string(self) -> String {
        let mut parts: Vec<&str> = Vec::new();
        if self.contains(Self::ALT) {
            parts.push("Alt");
        }
        if self.contains(Self::CONTROL) {
            parts.push("Control");
        }
        if self.contains(Self::SHIFT) {
            parts.push("Shift");
        }
        if self.contains(Self::WIN) {
            parts.push("Win");
        }
        if parts.is_empty() {
            "None".to_string()
        } else {
            parts.join(", ")
        }
    }
}

impl Serialize for ModifierKeys {
    fn serialize<S: serde::Serializer>(&self, serializer: S) -> Result<S::Ok, S::Error> {
        serializer.serialize_str(&self.to_csharp_string())
    }
}

impl<'de> Deserialize<'de> for ModifierKeys {
    fn deserialize<D: serde::Deserializer<'de>>(deserializer: D) -> Result<Self, D::Error> {
        let s = String::deserialize(deserializer)?;
        Ok(ModifierKeys::parse(&s))
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "PascalCase", default)]
pub struct AppSettings {
    pub hot_key_id: i32,
    pub hot_key: String,
    pub modifiers: ModifierKeys,
    pub api_key1: String,
    pub api_key2: String,
    pub api_key1_request_count: u32,
    pub api_key2_request_count: u32,
    pub api_key1_window_start: String,
    pub api_key2_window_start: String,
    pub language: String,
    pub app_language: String,
    pub mode: String,
    pub target_language: String,
    pub active_provider: SttProvider,
    pub local_model_name: String,
    pub use_gpu_acceleration: bool,
    pub whisper_prompt: String,
    pub border_color: String,
    pub border_opacity: u8,
    pub multicolor_border: bool,
    pub widget_pos_x: i32,
    pub widget_pos_y: i32,
}

impl Default for AppSettings {
    fn default() -> Self {
        Self {
            hot_key_id: 1,
            hot_key: "F8".into(),
            modifiers: ModifierKeys::NONE,
            api_key1: String::new(),
            api_key2: String::new(),
            api_key1_request_count: 0,
            api_key2_request_count: 0,
            api_key1_window_start: "0001-01-01T00:00:00Z".into(),
            api_key2_window_start: "0001-01-01T00:00:00Z".into(),
            language: "es".into(),
            app_language: "es".into(),
            mode: "transcription".into(),
            target_language: "en".into(),
            active_provider: SttProvider::GroqApi,
            local_model_name: "small".into(),
            use_gpu_acceleration: true,
            whisper_prompt: String::new(),
            border_color: "#FFFFFF".into(),
            border_opacity: 255,
            multicolor_border: false,
            widget_pos_x: 100,
            widget_pos_y: 100,
        }
    }
}

impl AppSettings {
    pub fn has_any_key(&self) -> bool {
        !self.api_key1.is_empty() || !self.api_key2.is_empty()
    }

    /// Legacy migration, mirrors SettingsService.MigrateSettings.
    fn migrate(&mut self) {
        if self.language != "auto" && !self.language.is_empty() && self.app_language == "es" {
            self.app_language = self.language.clone();
        }
        if self.app_language.is_empty() {
            self.app_language = "es".into();
        }
        if self.mode.is_empty() {
            self.mode = "transcription".into();
        }
        if self.target_language.is_empty() {
            self.target_language = "en".into();
        }
    }
}

pub struct SettingsService {
    inner: RwLock<AppSettings>,
    path: PathBuf,
}

impl SettingsService {
    pub fn new() -> Result<Self, String> {
        let dir = app_data_dir().ok_or("no local data dir")?;
        std::fs::create_dir_all(&dir).map_err(|e| e.to_string())?;
        let path = dir.join("settings.json");

        let settings: AppSettings = match std::fs::read_to_string(&path) {
            Ok(json) => serde_json::from_str(&json).unwrap_or_default(),
            Err(_) => AppSettings::default(),
        };
        let mut settings = settings;
        settings.migrate();

        Ok(Self {
            inner: RwLock::new(settings),
            path,
        })
    }

    pub fn get(&self) -> AppSettings {
        self.inner.read().unwrap().clone()
    }

    pub fn update<F: FnOnce(&mut AppSettings)>(&self, f: F) -> AppSettings {
        let mut guard = self.inner.write().unwrap();
        f(&mut guard);
        let snapshot = guard.clone();
        drop(guard);
        self.save(&snapshot);
        snapshot
    }

    fn save(&self, settings: &AppSettings) {
        match serde_json::to_string_pretty(settings) {
            Ok(json) => {
                if let Err(e) = std::fs::write(&self.path, json) {
                    log::warn!("failed to save settings: {e}");
                }
            }
            Err(e) => log::warn!("failed to serialize settings: {e}"),
        }
    }
}

/// Same location as the legacy app: `%LOCALAPPDATA%\VoiceToTextWidget`
/// (Windows), `~/.local/share/VoiceToTextWidget` (Linux),
/// `~/Library/Application Support/VoiceToTextWidget` (macOS).
pub fn app_data_dir() -> Option<PathBuf> {
    dirs::data_local_dir().map(|d| d.join(APP_DIR_NAME))
}

pub fn models_dir() -> Option<PathBuf> {
    app_data_dir().map(|d| d.join("Models"))
}

pub fn model_path(name: &str) -> Option<PathBuf> {
    models_dir().map(|d| d.join(format!("ggml-{name}.bin")))
}
