//! Microphone capture via cpal. Audio is mixed down to mono, accumulated at
//! the device's native rate as f32, and converted to 16 kHz mono i16 on stop
//! (the format whisper.cpp and the Groq API both expect — parity with the
//! legacy NAudio `WaveFormat(16000, 16, 1)` full-buffer pipeline).

use cpal::traits::{DeviceTrait, HostTrait, StreamTrait};
use cpal::{SampleFormat, Stream, StreamConfig};
use std::sync::atomic::{AtomicU32, Ordering};
use std::sync::{mpsc, Arc, Mutex};

pub const TARGET_SAMPLE_RATE: u32 = 16_000;
/// Minimum audio length accepted before calling STT: 0.25 s (parity with the
/// legacy `MinAudioBytes = 8000` at 16 kHz mono 16-bit).
pub const MIN_SAMPLES_16K: usize = (TARGET_SAMPLE_RATE as usize) / 4;

/// Commands sent to the dedicated audio worker thread. cpal `Stream` is
/// `!Send` on Windows, so capture lives on its own thread and is driven
/// through this channel.
pub enum AudioCmd {
    Start(mpsc::Sender<Result<(), String>>),
    Stop(mpsc::Sender<Vec<i16>>),
}

/// `Send + Sync` handle the rest of the app uses instead of AudioCapture.
pub struct AudioHandle {
    tx: Mutex<mpsc::Sender<AudioCmd>>,
    level: Arc<AtomicU32>,
}

impl Default for AudioHandle {
    fn default() -> Self {
        Self::new()
    }
}

impl AudioHandle {
    pub fn new() -> Self {
        let (tx, rx) = mpsc::channel::<AudioCmd>();
        let level = Arc::new(AtomicU32::new(0));
        let level_shared = Arc::clone(&level);
        std::thread::Builder::new()
            .name("audio-worker".into())
            .spawn(move || audio_worker(rx, level_shared))
            .expect("failed to spawn audio worker");
        Self {
            tx: Mutex::new(tx),
            level,
        }
    }

    pub fn start(&self) -> Result<(), String> {
        let (reply_tx, reply_rx) = mpsc::channel();
        self.tx
            .lock()
            .unwrap()
            .send(AudioCmd::Start(reply_tx))
            .map_err(|_| "el hilo de audio no está disponible".to_string())?;
        reply_rx
            .recv()
            .map_err(|_| "el hilo de audio no respondió".to_string())?
    }

    /// Blocking; returns the recording as 16 kHz mono i16 samples.
    pub fn stop(&self) -> Vec<i16> {
        let (reply_tx, reply_rx) = mpsc::channel();
        if self
            .tx
            .lock()
            .unwrap()
            .send(AudioCmd::Stop(reply_tx))
            .is_err()
        {
            return Vec::new();
        }
        reply_rx.recv().unwrap_or_default()
    }

    /// Latest input level, 0.0..1.0.
    pub fn level(&self) -> f32 {
        (self.level.load(Ordering::Relaxed) as f32 / 1000.0).sqrt()
    }
}

fn audio_worker(rx: mpsc::Receiver<AudioCmd>, level: Arc<AtomicU32>) {
    let mut capture: Option<AudioCapture> = None;
    for cmd in rx {
        match cmd {
            AudioCmd::Start(reply) => {
                let result = (|| -> Result<(), String> {
                    let cap = capture.get_or_insert_with(AudioCapture::new);
                    cap.level_slot = Some(Arc::clone(&level));
                    cap.start()
                })();
                let _ = reply.send(result);
            }
            AudioCmd::Stop(reply) => {
                let samples = capture
                    .as_mut()
                    .map(|c| {
                        let out = c.stop();
                        c.level_slot.as_ref().map(|l| l.store(0, Ordering::Relaxed));
                        out
                    })
                    .unwrap_or_default();
                let _ = reply.send(samples);
            }
        }
    }
}

pub struct AudioCapture {
    stream: Option<Stream>,
    buffer: Arc<Mutex<Vec<f32>>>,
    /// Latest input peak, 0..1000 (atomic so the realtime callback never locks).
    level_slot: Option<Arc<AtomicU32>>,
    channels: usize,
    sample_rate: u32,
}

impl AudioCapture {
    pub fn new() -> Self {
        Self {
            stream: None,
            buffer: Arc::new(Mutex::new(Vec::new())),
            level_slot: None,
            channels: 1,
            sample_rate: TARGET_SAMPLE_RATE,
        }
    }

    pub fn start(&mut self) -> Result<(), String> {
        if self.stream.is_some() {
            return Ok(());
        }

        let host = cpal::default_host();
        let device = host
            .default_input_device()
            .ok_or_else(|| "No se encontró ningún micrófono".to_string())?;

        let supported = device
            .default_input_config()
            .map_err(|e| format!("No se pudo leer la configuración del micrófono: {e}"))?;
        let sample_format = supported.sample_format();
        let config: StreamConfig = supported.into();

        self.channels = config.channels.max(1) as usize;
        self.sample_rate = config.sample_rate.0;
        self.buffer.lock().unwrap().clear();

        let buffer = Arc::clone(&self.buffer);
        let level = self
            .level_slot
            .as_ref()
            .cloned()
            .unwrap_or_else(|| Arc::new(AtomicU32::new(0)));
        let channels = self.channels;

        let err_fn = |e| log::warn!("audio stream error: {e}");

        let stream = match sample_format {
            SampleFormat::F32 => device.build_input_stream(
                &config,
                move |data: &[f32], _| feed(data, channels, &buffer, &level),
                err_fn,
                None,
            ),
            SampleFormat::I16 => device.build_input_stream(
                &config,
                move |data: &[i16], _| {
                    let f32_data: Vec<f32> = data.iter().map(|s| *s as f32 / 32768.0).collect();
                    feed(&f32_data, channels, &buffer, &level)
                },
                err_fn,
                None,
            ),
            SampleFormat::U16 => device.build_input_stream(
                &config,
                move |data: &[u16], _| {
                    let f32_data: Vec<f32> = data
                        .iter()
                        .map(|s| (*s as f32 - 32768.0) / 32768.0)
                        .collect();
                    feed(&f32_data, channels, &buffer, &level)
                },
                err_fn,
                None,
            ),
            other => return Err(format!("Formato de audio no soportado: {other:?}")),
        }
        .map_err(|e| format!("No se pudo iniciar la captura: {e}"))?;

        stream
            .play()
            .map_err(|e| format!("No se pudo reproducir el stream de captura: {e}"))?;

        self.stream = Some(stream);
        Ok(())
    }

    /// Stops capture and returns the recording as 16 kHz mono i16 samples.
    pub fn stop(&mut self) -> Vec<i16> {
        if let Some(stream) = self.stream.take() {
            // Dropping the stream waits for the callback to finish.
            drop(stream);
        }
        let mono = std::mem::take(&mut *self.buffer.lock().unwrap());
        resample_to_16k(&mono, self.sample_rate)
    }
}

fn feed(data: &[f32], channels: usize, buffer: &Mutex<Vec<f32>>, level: &AtomicU32) {
    if data.is_empty() {
        return;
    }
    let frames = data.len() / channels.max(1);
    let mut mono = Vec::with_capacity(frames);
    let mut peak: f32 = 0.0;
    for frame in 0..frames {
        let start = frame * channels;
        let mut sum = 0.0f32;
        for ch in 0..channels {
            let s = data[start + ch].clamp(-1.0, 1.0);
            sum += s;
            let abs = s.abs();
            if abs > peak {
                peak = abs;
            }
        }
        mono.push(sum / channels as f32);
    }
    level.store((peak.min(1.0) * 1000.0) as u32, Ordering::Relaxed);
    buffer.lock().unwrap().extend_from_slice(&mono);
}

/// Linear-interpolation resampler, native rate → 16 kHz mono i16.
pub fn resample_to_16k(input: &[f32], from_rate: u32) -> Vec<i16> {
    if input.is_empty() {
        return Vec::new();
    }
    if from_rate == TARGET_SAMPLE_RATE {
        return input.iter().map(|s| to_i16(*s)).collect();
    }
    let ratio = from_rate as f64 / TARGET_SAMPLE_RATE as f64;
    let out_len = ((input.len() as f64) / ratio).floor() as usize;
    let mut out = Vec::with_capacity(out_len);
    for i in 0..out_len {
        let src = i as f64 * ratio;
        let i0 = src.floor() as usize;
        let i1 = (i0 + 1).min(input.len() - 1);
        let frac = (src - i0 as f64) as f32;
        let sample = input[i0] * (1.0 - frac) + input[i1] * frac;
        out.push(to_i16(sample));
    }
    out
}

fn to_i16(sample: f32) -> i16 {
    (sample.clamp(-1.0, 1.0) * 32767.0) as i16
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn resample_identity_when_already_16k() {
        let input = vec![0.0f32, 0.5, -0.5, 1.0];
        let out = resample_to_16k(&input, 16_000);
        assert_eq!(out, input.iter().map(|s| to_i16(*s)).collect::<Vec<_>>());
    }

    #[test]
    fn resample_halves_32k_to_16k() {
        let input = vec![0.0f32; 1000];
        let out = resample_to_16k(&input, 32_000);
        assert_eq!(out.len(), 500);
    }

    #[test]
    fn resample_clamps_amplitude() {
        let input = vec![2.0f32, -2.0];
        let out = resample_to_16k(&input, 16_000);
        assert_eq!(out[0], i16::MAX);
        assert_eq!(out[1], -i16::MAX);
    }

    #[test]
    fn empty_input_gives_empty_output() {
        assert!(resample_to_16k(&[], 48_000).is_empty());
    }
}

#[cfg(test)]
mod device_probe {
    use cpal::traits::{DeviceTrait, HostTrait};
    #[test]
    fn probe_cpal_devices() {
        let host = cpal::default_host();
        println!("host id: {:?}", host.id());
        match host.input_devices() {
            Ok(devices) => {
                for d in devices {
                    println!("input: {:?}", d.name());
                }
            }
            Err(e) => println!("input_devices error: {e}"),
        }
        println!(
            "default input: {:?}",
            host.default_input_device().map(|d| d.name().ok())
        );
    }
}
