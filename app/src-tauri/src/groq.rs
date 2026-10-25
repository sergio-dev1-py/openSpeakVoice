//! Groq API client — parity with the legacy GroqApiSpeechService:
//! * transcription: `whisper-large-v3` via /audio/transcriptions (WAV upload)
//! * translation: `openai/gpt-oss-120b` via /chat/completions, with
//!   `openai/gpt-oss-20b` as an automatic fallback if the first is unavailable
//! * dual API keys with per-minute usage windows (19 req/min each), rotated
//!   to the least-used key and persisted in settings.json.

use crate::languages;
use crate::settings::SettingsService;
use reqwest::header::AUTHORIZATION;
use serde_json::{json, Value};
use std::time::{Duration, SystemTime, UNIX_EPOCH};

pub const BASE_URL: &str = "https://api.groq.com/openai/v1";
pub const TRANSCRIBE_MODEL: &str = "whisper-large-v3";
pub const TRANSLATE_MODELS: &[&str] = &["openai/gpt-oss-120b", "openai/gpt-oss-20b"];
pub const MAX_REQUESTS_PER_MINUTE: u32 = 19;
const HTTP_TIMEOUT: Duration = Duration::from_secs(30);

/// Encode 16 kHz mono i16 PCM as a canonical 44-byte-header WAV (parity with
/// the hand-written header in the legacy services).
pub fn pcm_to_wav(samples: &[i16]) -> Vec<u8> {
    let sample_rate: u32 = 16_000;
    let channels: u16 = 1;
    let bits: u16 = 16;
    let byte_rate = sample_rate * channels as u32 * (bits / 8) as u32;
    let block_align = channels * (bits / 8);
    let data_len = (samples.len() * 2) as u32;

    let mut out = Vec::with_capacity(44 + data_len as usize);
    out.extend_from_slice(b"RIFF");
    out.extend_from_slice(&(36 + data_len).to_le_bytes());
    out.extend_from_slice(b"WAVE");
    out.extend_from_slice(b"fmt ");
    out.extend_from_slice(&16u32.to_le_bytes());
    out.extend_from_slice(&1u16.to_le_bytes()); // PCM
    out.extend_from_slice(&channels.to_le_bytes());
    out.extend_from_slice(&sample_rate.to_le_bytes());
    out.extend_from_slice(&byte_rate.to_le_bytes());
    out.extend_from_slice(&block_align.to_le_bytes());
    out.extend_from_slice(&bits.to_le_bytes());
    out.extend_from_slice(b"data");
    out.extend_from_slice(&data_len.to_le_bytes());
    for s in samples {
        out.extend_from_slice(&s.to_le_bytes());
    }
    out
}

fn client() -> Result<reqwest::Client, String> {
    reqwest::Client::builder()
        .timeout(HTTP_TIMEOUT)
        .build()
        .map_err(|e| format!("No se pudo crear el cliente HTTP: {e}"))
}

fn window_start_epoch(raw: &str) -> u64 {
    // Legacy values are RFC3339 ("0001-01-01T00:00:00Z"); we only need rough
    // epoch math, so parse `YYYY-MM-DDTHH:MM:SSZ` manually.
    let b = raw.as_bytes();
    if b.len() < 20 || b[4] != b'-' {
        return 0;
    }
    let num = |r: std::ops::Range<usize>| -> i64 {
        std::str::from_utf8(&b[r])
            .ok()
            .and_then(|s| s.parse::<i64>().ok())
            .unwrap_or(0)
    };
    let (y, mo, d) = (num(0..4), num(5..7), num(8..10));
    let (h, mi, s) = (num(11..13), num(14..16), num(17..19));
    // Days since 1970 via civil-from-days (Howard Hinnant's algorithm).
    let y = if mo <= 2 { y - 1 } else { y };
    let era = if y >= 0 { y } else { y - 399 } / 400;
    let yoe = y - era * 400;
    let mp = (mo + 9) % 12;
    let doy = (153 * mp + 2) / 5 + d - 1;
    let doe = yoe * 365 + yoe / 4 - yoe / 100 + doy;
    let days = era * 146_097 + doe - 71_946;
    let secs = days * 86_400 + h * 3600 + mi * 60 + s;
    if secs < 0 {
        0
    } else {
        secs as u64
    }
}

fn epoch_to_iso(epoch: u64) -> String {
    let secs = epoch % 86_400;
    let days = (epoch / 86_400) as i64;
    let (h, mi, s) = (secs / 3600, (secs % 3600) / 60, secs % 60);
    // civil_from_days
    let z = days + 719_468;
    let era = if z >= 0 { z } else { z - 146_096 } / 146_097;
    let doe = z - era * 146_097;
    let yoe = (doe - doe / 1460 + doe / 36_524 - doe / 146_096) / 365;
    let y = yoe + era * 400;
    let doy = doe - (365 * yoe + yoe / 4 - yoe / 100);
    let mp = (5 * doy + 2) / 153;
    let d = doy - (153 * mp + 2) / 5 + 1;
    let m = if mp < 10 { mp + 3 } else { mp - 9 };
    let y = if m <= 2 { y + 1 } else { y };
    format!("{y:04}-{m:02}-{d:02}T{h:02}:{mi:02}:{s:02}Z")
}

pub struct KeyChoice {
    pub index: usize, // 1 or 2
    pub key: String,
}

/// Picks the key to use (least used; window reset after 60 s), mirroring the
/// legacy ApiKeyManager. `Err` carries the rate-limit message.
pub fn pick_key(settings: &crate::settings::AppSettings) -> Result<KeyChoice, String> {
    let now = SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .map(|d| d.as_secs())
        .unwrap_or(0);

    let candidates: [(usize, &str); 2] = [(1, &settings.api_key1), (2, &settings.api_key2)];

    let mut best: Option<KeyChoice> = None;
    let mut exhausted: Vec<usize> = Vec::new();
    for (idx, key) in candidates {
        if key.is_empty() {
            continue;
        }
        let (count, window) = if idx == 1 {
            (
                settings.api_key1_request_count,
                &settings.api_key1_window_start,
            )
        } else {
            (
                settings.api_key2_request_count,
                &settings.api_key2_window_start,
            )
        };
        let elapsed = now.saturating_sub(window_start_epoch(window));
        if elapsed >= 60 {
            // window expired → fresh start (lowest index wins, like legacy)
            return Ok(KeyChoice {
                index: idx,
                key: key.to_string(),
            });
        }
        if count < MAX_REQUESTS_PER_MINUTE {
            let choice = KeyChoice {
                index: idx,
                key: key.to_string(),
            };
            match &best {
                Some(current) => {
                    // prefer the less-used key (ties → lower index, matching legacy)
                    let current_count = if current.index == 1 {
                        settings.api_key1_request_count
                    } else {
                        settings.api_key2_request_count
                    };
                    if count < current_count {
                        best = Some(choice);
                    }
                }
                None => best = Some(choice),
            }
        } else {
            exhausted.push(idx);
        }
    }

    if let Some(choice) = best {
        return Ok(choice);
    }
    let msg = match exhausted.as_slice() {
        [idx] => format!("API Key {idx} alcanzó su límite de {MAX_REQUESTS_PER_MINUTE} req/min. Espera ~60 s para el reinicio."),
        [] => "No hay API keys configuradas".to_string(),
        _ => format!("Ambas API keys alcanzaron su límite de {MAX_REQUESTS_PER_MINUTE} req/min. Espera ~60 s para el reinicio."),
    };
    Err(msg)
}

/// Marks a successful request: count +1, window start = now if it was fresh.
pub fn record_usage(settings_service: &SettingsService, choice: &KeyChoice) {
    let now = SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .map(|d| d.as_secs())
        .unwrap_or(0);
    settings_service.update(|s| {
        let (count, window) = if choice.index == 1 {
            (&mut s.api_key1_request_count, &mut s.api_key1_window_start)
        } else {
            (&mut s.api_key2_request_count, &mut s.api_key2_window_start)
        };
        let elapsed = now.saturating_sub(window_start_epoch(window));
        if elapsed >= 60 {
            *count = 1;
            *window = epoch_to_iso(now);
        } else {
            *count = count.saturating_add(1);
        }
    });
}

/// Current usage status for the keys dialog: (used1, total, used2, total)
/// with window resets applied the same way the legacy dialog computed them.
pub fn usage_status(settings: &crate::settings::AppSettings) -> (u32, u32, u32, u32) {
    let now = SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .map(|d| d.as_secs())
        .unwrap_or(0);
    let eff = |count: u32, window: &str| -> u32 {
        if now.saturating_sub(window_start_epoch(window)) >= 60 {
            0
        } else {
            count
        }
    };
    (
        eff(
            settings.api_key1_request_count,
            &settings.api_key1_window_start,
        ),
        MAX_REQUESTS_PER_MINUTE,
        eff(
            settings.api_key2_request_count,
            &settings.api_key2_window_start,
        ),
        MAX_REQUESTS_PER_MINUTE,
    )
}

/// Transcribes WAV bytes with `whisper-large-v3`. Returns the recognized text.
pub async fn transcribe(
    api_key: &str,
    wav: Vec<u8>,
    language: Option<&str>,
) -> Result<String, String> {
    let http = client()?;
    let part = reqwest::multipart::Part::bytes(wav)
        .file_name("audio.wav")
        .mime_str("audio/wav")
        .map_err(|e| e.to_string())?;
    let mut form = reqwest::multipart::Form::new()
        .part("file", part)
        .text("model", TRANSCRIBE_MODEL.to_string())
        .text("temperature", "0");
    if let Some(lang) = language {
        form = form.text("language", lang.to_string());
    }

    let response = http
        .post(format!("{BASE_URL}/audio/transcriptions"))
        .header(AUTHORIZATION, format!("Bearer {api_key}"))
        .multipart(form)
        .send()
        .await
        .map_err(|e| format!("Error de conexión con Groq: {e}"))?;

    let status = response.status();
    let body = response.text().await.unwrap_or_default();
    if !status.is_success() {
        return Err(format!("Groq respondió {status}: {body}"));
    }
    let json: Value =
        serde_json::from_str(&body).map_err(|e| format!("Respuesta inválida de Groq: {e}"))?;
    Ok(json["text"].as_str().unwrap_or_default().trim().to_string())
}

/// Translates text to `target_code`. Tries `openai/gpt-oss-120b` first and
/// falls back to `openai/gpt-oss-20b` if the model is missing or not available
/// for the current API key.
pub async fn translate(api_key: &str, text: &str, target_code: &str) -> Result<String, String> {
    let target_name = languages::display_name(target_code).unwrap_or(target_code);
    let system = format!(
        "Eres un traductor profesional. Traduce el siguiente texto al idioma {target_name} ({target_code}). \
Responde SOLO con la traducción, sin explicaciones, sin comillas, sin texto adicional."
    );

    let http = client()?;
    let mut last_error = String::from("No hay modelos de traducción configurados");

    for (index, model) in TRANSLATE_MODELS.iter().enumerate() {
        let response = http
            .post(format!("{BASE_URL}/chat/completions"))
            .header(AUTHORIZATION, format!("Bearer {api_key}"))
            .json(&json!({
                "model": *model,
                "temperature": 0.3,
                "messages": [
                    { "role": "system", "content": system.as_str() },
                    { "role": "user", "content": text }
                ]
            }))
            .send()
            .await
            .map_err(|e| format!("Error de conexión con Groq: {e}"))?;

        let status = response.status();
        let body = response.text().await.unwrap_or_default();
        if status.is_success() {
            let json: Value = serde_json::from_str(&body)
                .map_err(|e| format!("Respuesta inválida de Groq: {e}"))?;
            return Ok(json["choices"][0]["message"]["content"]
                .as_str()
                .unwrap_or(text)
                .trim()
                .to_string());
        }

        let lower = body.to_ascii_lowercase();
        let model_unavailable = status == reqwest::StatusCode::NOT_FOUND
            || lower.contains("does not exist")
            || lower.contains("not have access")
            || lower.contains("decommissioned")
            || lower.contains("model_not_found");
        last_error = format!("Groq respondió {status} con {model}: {body}");

        if model_unavailable && index + 1 < TRANSLATE_MODELS.len() {
            log::warn!("translation model {model} unavailable, trying fallback: {body}");
            continue;
        }
        if model_unavailable {
            return Err(format!(
                "Ningún modelo de traducción de Groq está disponible. Detalle: {last_error}"
            ));
        }
        return Err(last_error);
    }

    Err(format!(
        "Ningún modelo de traducción de Groq está disponible. {last_error}"
    ))
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn wav_header_matches_legacy_format() {
        let wav = pcm_to_wav(&[1, -1, 0x7FFF]);
        assert_eq!(wav.len(), 44 + 6);
        assert_eq!(&wav[0..4], b"RIFF");
        assert_eq!(&wav[8..12], b"WAVE");
        assert_eq!(&wav[12..16], b"fmt ");
        assert_eq!(u32::from_le_bytes(wav[16..20].try_into().unwrap()), 16);
        assert_eq!(u16::from_le_bytes(wav[20..22].try_into().unwrap()), 1); // PCM
        assert_eq!(u16::from_le_bytes(wav[22..24].try_into().unwrap()), 1); // mono
        assert_eq!(u32::from_le_bytes(wav[24..28].try_into().unwrap()), 16_000);
        assert_eq!(u16::from_le_bytes(wav[32..34].try_into().unwrap()), 2); // block align
        assert_eq!(u16::from_le_bytes(wav[34..36].try_into().unwrap()), 16); // bits
        assert_eq!(&wav[36..40], b"data");
        assert_eq!(u32::from_le_bytes(wav[40..44].try_into().unwrap()), 6);
    }

    #[test]
    fn legacy_window_start_parses_to_zero() {
        assert_eq!(window_start_epoch("0001-01-01T00:00:00Z"), 0);
    }

    #[test]
    fn iso_roundtrip_today() {
        let iso = epoch_to_iso(1_700_000_000); // 2023-11-14T22:13:20Z
        assert_eq!(iso, "2023-11-14T22:13:20Z");
        assert!(window_start_epoch(&iso) > 1_600_000_000);
    }
}
