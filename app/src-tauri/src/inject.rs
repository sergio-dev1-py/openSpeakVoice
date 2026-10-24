//! Text injection into the focused app — parity with the legacy
//! TextInjectionService: clipboard paste first (Ctrl+V / Cmd+V), per-character
//! typing as last resort. The pill window never takes focus, so the paste
//! lands in whatever the user had focused.

use enigo::{Direction, Enigo, Key, Keyboard};

pub fn inject_text(text: &str) -> Result<(), String> {
    paste_via_clipboard(text).or_else(|paste_err| {
        log::warn!("clipboard paste failed ({paste_err}), falling back to typing");
        type_text(text).map_err(|type_err| {
            format!("No se pudo insertar el texto (portapapeles: {paste_err}; tipeo: {type_err})")
        })
    })
}

fn paste_via_clipboard(text: &str) -> Result<(), String> {
    let mut clipboard =
        arboard::Clipboard::new().map_err(|e| format!("no se pudo abrir el portapapeles: {e}"))?;
    clipboard
        .set_text(text.to_string())
        .map_err(|e| format!("no se pudo escribir el portapapeles: {e}"))?;
    drop(clipboard);

    std::thread::sleep(std::time::Duration::from_millis(30));

    let mut enigo =
        Enigo::new(&enigo::Settings::default()).map_err(|e| format!("no se pudo iniciar la entrada sintética: {e}"))?;
    #[cfg(target_os = "macos")]
    let modifier = Key::Meta;
    #[cfg(not(target_os = "macos"))]
    let modifier = Key::Control;

    enigo
        .key(modifier, Direction::Press)
        .map_err(|e| format!("no se pudo presionar el modificador: {e}"))?;
    std::thread::sleep(std::time::Duration::from_millis(10));
    enigo
        .key(Key::Unicode('v'), Direction::Click)
        .map_err(|e| format!("no se pudo presionar V: {e}"))?;
    std::thread::sleep(std::time::Duration::from_millis(10));
    enigo
        .key(modifier, Direction::Release)
        .map_err(|e| format!("no se pudo soltar el modificador: {e}"))?;
    Ok(())
}

fn type_text(text: &str) -> Result<(), String> {
    let mut enigo =
        Enigo::new(&enigo::Settings::default()).map_err(|e| format!("no se pudo iniciar la entrada sintética: {e}"))?;
    enigo
        .text(text)
        .map_err(|e| format!("no se pudo tipear el texto: {e}"))
}
