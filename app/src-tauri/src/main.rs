// Keep the debug build console-free too: this is a desktop widget and must
// never create a console window in the taskbar. Diagnostics go to app.log.
#![cfg_attr(windows, windows_subsystem = "windows")]

fn main() -> Result<(), slint::PlatformError> {
    voicetotext_lib::run()
}
