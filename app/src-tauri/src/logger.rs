//! Minimal file logger: appends to `<app data>/VoiceToTextWidget/app.log`.
//! Keeps the log crate-based code paths observable without extra deps.

use log::{Level, Metadata, Record};
use std::io::Write;
use std::path::PathBuf;
use std::sync::Mutex;

pub fn init(dir: Option<PathBuf>) {
    FileLogger::init(dir);
}

pub struct FileLogger {
    file: Mutex<Option<std::fs::File>>,
}

impl FileLogger {
    pub fn init(dir: Option<PathBuf>) {
        let file = dir.and_then(|d| {
            std::fs::create_dir_all(&d).ok()?;
            std::fs::OpenOptions::new()
                .create(true)
                .append(true)
                .open(d.join("app.log"))
                .ok()
        });
        let logger = Box::new(Self {
            file: Mutex::new(file),
        });
        let _ = log::set_boxed_logger(logger);
        log::set_max_level(log::LevelFilter::Info);
    }
}

impl log::Log for FileLogger {
    fn enabled(&self, metadata: &Metadata) -> bool {
        metadata.level() <= Level::Info
    }

    fn log(&self, record: &Record) {
        if !self.enabled(record.metadata()) {
            return;
        }
        let mut guard = self.file.lock().unwrap();
        if let Some(file) = guard.as_mut() {
            let _ = writeln!(
                file,
                "[{}] [{}] {}",
                chrono_now(),
                record.level(),
                record.args()
            );
        }
    }

    fn flush(&self) {}
}

/// Local timestamp without pulling chrono: seconds since epoch formatted.
fn chrono_now() -> String {
    let secs = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .map(|d| d.as_secs())
        .unwrap_or(0);
    let days = (secs / 86_400) as i64;
    let tod = secs % 86_400;
    let (h, m, s) = (tod / 3600, (tod % 3600) / 60, tod % 60);
    // civil_from_days (Hinnant)
    let z = days + 719_468;
    let era = if z >= 0 { z } else { z - 146_096 } / 146_097;
    let doe = z - era * 146_097;
    let yoe = (doe - doe / 1460 + doe / 36_524 - doe / 146_096) / 365;
    let y = yoe + era * 400;
    let doy = doe - (365 * yoe + yoe / 4 - yoe / 100);
    let mp = (5 * doy + 2) / 153;
    let d = doy - (153 * mp + 2) / 5 + 1;
    let mo = if mp < 10 { mp + 3 } else { mp - 9 };
    let y = if mo <= 2 { y + 1 } else { y };
    format!("{y:04}-{mo:02}-{d:02} {h:02}:{m:02}:{s:02}")
}
