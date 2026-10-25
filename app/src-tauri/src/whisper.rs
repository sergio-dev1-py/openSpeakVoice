//! Local Whisper (whisper.cpp via whisper-rs) + ggml model management.
//! Models live in `<app data>/VoiceToTextWidget/Models/ggml-<name>.bin`, the
//! same files the legacy C# app downloaded from HuggingFace.

use crate::settings::model_path;
use serde::Serialize;
use std::path::PathBuf;
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::Mutex;
use whisper_rs::{FullParams, SamplingStrategy, WhisperContext, WhisperContextParameters};

pub const GROQ_FALLBACK_HINT: &str = "https://huggingface.co/ggerganov/whisper.cpp/resolve/main";

#[derive(Debug, Clone, Serialize)]
pub struct ModelInfo {
    pub name: &'static str,
    pub label: &'static str,
    pub size_label: &'static str,
    pub downloaded: bool,
    pub size_on_disk: Option<u64>,
}

pub const MODELS: &[(&str, &str, &str)] = &[
    ("tiny", "Tiny", "~75 MB"),
    ("base", "Base", "~142 MB"),
    ("small", "Small", "~466 MB"),
    ("medium", "Medium", "~1.5 GB"),
    ("large-v3", "Large v3", "~3.1 GB"),
];

pub fn model_url(name: &str) -> String {
    format!("{GROQ_FALLBACK_HINT}/ggml-{name}.bin")
}

pub fn list_models() -> Vec<ModelInfo> {
    MODELS
        .iter()
        .map(|(name, label, size_label)| {
            let path = model_path(name);
            let size_on_disk = path
                .as_ref()
                .and_then(|p| std::fs::metadata(p).ok())
                .map(|m| m.len());
            ModelInfo {
                name,
                label,
                size_label,
                downloaded: size_on_disk.is_some(),
                size_on_disk,
            }
        })
        .collect()
}

pub fn delete_model(name: &str) -> Result<(), String> {
    let path = model_path(name).ok_or("ruta inválida")?;
    if path.exists() {
        std::fs::remove_file(&path).map_err(|e| format!("No se pudo borrar el modelo: {e}"))?;
    }
    Ok(())
}

/// Shared cancel flag so the models window can abort an in-flight download.
pub struct DownloadGuard(pub AtomicBool);

impl Default for DownloadGuard {
    fn default() -> Self {
        Self(AtomicBool::new(false))
    }
}

/// Streams a model to disk, reporting progress through `on_progress`
/// (`downloaded, total, percent`) and honoring `guard` cancellation.
pub async fn download_model<F>(
    guard: std::sync::Arc<DownloadGuard>,
    name: &str,
    on_progress: F,
) -> Result<(), String>
where
    F: Fn(u64, Option<u64>, u32) + Send + Sync,
{
    if !MODELS.iter().any(|(n, _, _)| *n == name) {
        return Err(format!("Modelo desconocido: {name}"));
    }
    let path: PathBuf = model_path(name).ok_or("ruta inválida")?;
    std::fs::create_dir_all(path.parent().unwrap()).map_err(|e| e.to_string())?;
    // Partial files keep a .part suffix until complete.
    let tmp = path.with_extension("bin.part");

    guard.0.store(false, Ordering::SeqCst);

    let url = model_url(name);
    let response = reqwest::get(&url)
        .await
        .and_then(|r| r.error_for_status())
        .map_err(|e| format!("Error descargando el modelo: {e}"))?;

    let total = response.content_length();
    let mut file = tokio::fs::File::create(&tmp)
        .await
        .map_err(|e| format!("No se pudo crear el archivo: {e}"))?;

    use futures_util::StreamExt;
    use tokio::io::AsyncWriteExt;

    let mut stream = response.bytes_stream();
    let mut downloaded: u64 = 0;
    let mut last_emitted: u64 = 0;

    while let Some(chunk) = stream.next().await {
        if guard.0.load(Ordering::SeqCst) {
            drop(file);
            let _ = tokio::fs::remove_file(&tmp).await;
            return Err("Descarga cancelada".into());
        }
        let chunk = chunk.map_err(|e| format!("Error de red: {e}"))?;
        file.write_all(&chunk)
            .await
            .map_err(|e| format!("Error escribiendo el archivo: {e}"))?;
        downloaded += chunk.len() as u64;
        if downloaded - last_emitted >= 1_000_000 {
            last_emitted = downloaded;
            let percent = total
                .map(|t| (downloaded as f64 / t as f64 * 100.0) as u32)
                .unwrap_or(0);
            on_progress(downloaded, total, percent);
        }
    }
    file.flush().await.map_err(|e| e.to_string())?;
    drop(file);
    tokio::fs::rename(&tmp, &path)
        .await
        .map_err(|e| format!("No se pudo finalizar la descarga: {e}"))?;
    on_progress(downloaded, total, 100);
    Ok(())
}

/// Owns a loaded whisper context; the model is kept in memory between
/// transcriptions (parity with the legacy singleton factory).
pub struct WhisperEngine {
    context: Option<WhisperContext>,
    loaded_model: String,
}

impl Default for WhisperEngine {
    fn default() -> Self {
        Self {
            context: None,
            loaded_model: String::new(),
        }
    }
}

impl WhisperEngine {
    pub fn reset(&mut self) {
        self.context = None;
        self.loaded_model.clear();
    }

    pub fn loaded_model(&self) -> &str {
        &self.loaded_model
    }

    fn ensure_loaded(&mut self, model_name: &str) -> Result<&WhisperContext, String> {
        if self.context.is_some() && self.loaded_model == model_name {
            return Ok(self.context.as_ref().unwrap());
        }
        self.context = None;
        self.loaded_model.clear();

        let path = model_path(model_name).ok_or("ruta inválida")?;
        if !path.exists() {
            return Err(format!(
                "El modelo {model_name} no está descargado. Ábrelo desde Proveedor → Modelos."
            ));
        }
        let ctx = WhisperContext::new_with_params(
            path.to_string_lossy().as_ref(),
            WhisperContextParameters::default(),
        )
        .map_err(|e| format!("No se pudo cargar el modelo {model_name}: {e}"))?;
        self.loaded_model = model_name.to_string();
        self.context = Some(ctx);
        Ok(self.context.as_ref().unwrap())
    }

    /// Runs transcription. Blocking — call from `spawn_blocking`.
    pub fn transcribe(
        &mut self,
        samples: &[i16],
        model_name: &str,
        language: Option<&str>,
    ) -> Result<String, String> {
        let ctx = self.ensure_loaded(model_name)?;
        let mut state = ctx
            .create_state()
            .map_err(|e| format!("No se pudo crear el estado de whisper: {e}"))?;

        let mut params = FullParams::new(SamplingStrategy::Greedy { best_of: 1 });
        params.set_temperature(0.0);
        params.set_no_context(true);
        params.set_suppress_blank(true);
        params.set_suppress_nst(true);
        if let Some(lang) = language {
            params.set_language(Some(lang));
        }

        // whisper-rs expects f32 PCM at 16 kHz mono.
        let samples_f32: Vec<f32> = samples.iter().map(|s| *s as f32 / 32768.0).collect();
        state
            .full(params, &samples_f32)
            .map_err(|e| format!("Error durante la transcripción: {e}"))?;

        let n = state.full_n_segments();
        let mut text = String::new();
        for i in 0..n {
            if let Some(seg) = state.get_segment(i) {
                if let Ok(seg_text) = seg.to_str_lossy() {
                    text.push_str(&seg_text);
                    text.push(' ');
                }
            }
        }
        Ok(text.trim().to_string())
    }
}

/// Global guard registry for download cancellation (shared with commands).
pub type SharedGuard = Mutex<std::sync::Arc<DownloadGuard>>;

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn model_urls_match_legacy_source() {
        assert_eq!(
            model_url("small"),
            "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin"
        );
    }

    #[test]
    fn unknown_model_rejected() {
        assert!(!MODELS.iter().any(|(n, _, _)| *n == "nope"));
    }
}
