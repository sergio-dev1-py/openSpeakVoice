//! Global hotkey registration. Settings store the hotkey in legacy C# `Keys`
//! names ("F8", "A", "D1", "Oem3"…); this maps them to W3C `Code` names used
//! by the global-hotkey/tauri-plugin-global-shortcut ecosystem.

use crate::settings::ModifierKeys;

/// C# Keys name → W3C KeyboardEvent.code name (as parsed by global-hotkey).
pub fn code_name(key: &str) -> Option<&'static str> {
    const FKEYS: [&str; 24] = [
        "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12", "F13", "F14",
        "F15", "F16", "F17", "F18", "F19", "F20", "F21", "F22", "F23", "F24",
    ];
    let k = key.trim();
    // F-keys
    if let Some(rest) = k.strip_prefix('F') {
        if let Ok(n) = rest.parse::<u32>() {
            if (1..=24).contains(&n) {
                return Some(FKEYS[(n - 1) as usize]);
            }
        }
    }
    // Letters
    if k.len() == 1 {
        let c = k.chars().next().unwrap().to_ascii_uppercase();
        if c.is_ascii_uppercase() {
            return Some(match c {
                'A' => "KeyA",
                'B' => "KeyB",
                'C' => "KeyC",
                'D' => "KeyD",
                'E' => "KeyE",
                'F' => "KeyF",
                'G' => "KeyG",
                'H' => "KeyH",
                'I' => "KeyI",
                'J' => "KeyJ",
                'K' => "KeyK",
                'L' => "KeyL",
                'M' => "KeyM",
                'N' => "KeyN",
                'O' => "KeyO",
                'P' => "KeyP",
                'Q' => "KeyQ",
                'R' => "KeyR",
                'S' => "KeyS",
                'T' => "KeyT",
                'U' => "KeyU",
                'V' => "KeyV",
                'W' => "KeyW",
                'X' => "KeyX",
                'Y' => "KeyY",
                _ => "KeyZ",
            });
        }
    }
    // Key name table (static; the trimmed input is only matched against).
    let matched: &str = k;
    let result = match matched {
        "D0" => "Digit0",
        "D1" => "Digit1",
        "D2" => "Digit2",
        "D3" => "Digit3",
        "D4" => "Digit4",
        "D5" => "Digit5",
        "D6" => "Digit6",
        "D7" => "Digit7",
        "D8" => "Digit8",
        "D9" => "Digit9",
        "NumPad0" => "Numpad0",
        "NumPad1" => "Numpad1",
        "NumPad2" => "Numpad2",
        "NumPad3" => "Numpad3",
        "NumPad4" => "Numpad4",
        "NumPad5" => "Numpad5",
        "NumPad6" => "Numpad6",
        "NumPad7" => "Numpad7",
        "NumPad8" => "Numpad8",
        "NumPad9" => "Numpad9",
        "Space" => "Space",
        "Enter" | "Return" => "Enter",
        "Escape" => "Escape",
        "Tab" => "Tab",
        "Back" | "Backspace" => "Backspace",
        "Delete" => "Delete",
        "Insert" => "Insert",
        "Home" => "Home",
        "End" => "End",
        "Prior" | "PageUp" => "PageUp",
        "Next" | "PageDown" => "PageDown",
        "Up" => "ArrowUp",
        "Down" => "ArrowDown",
        "Left" => "ArrowLeft",
        "Right" => "ArrowRight",
        "Oemtilde" | "OemTilde" => "Backquote",
        "OemMinus" => "Minus",
        "Oemplus" => "Equal",
        "OemOpenBrackets" => "BracketLeft",
        "OemCloseBrackets" => "BracketRight",
        "OemPipe" => "Backslash",
        "OemSemicolon" => "Semicolon",
        "OemQuotes" => "Quote",
        "Oemcomma" | "OemComma" => "Comma",
        "OemPeriod" => "Period",
        "OemQuestion" => "Slash",
        "Multiply" => "NumpadMultiply",
        "Add" => "NumpadAdd",
        "Subtract" => "NumpadSubtract",
        "Divide" => "NumpadDivide",
        "Decimal" => "NumpadDecimal",
        _ => return None,
    };
    Some(result)
}

/// Builds the shortcut string accepted by `Shortcut::from_str`, e.g.
/// "Control + Alt + F8" → "Control+Alt+F8". Returns `None` for unknown keys.
pub fn shortcut_string(key: &str, modifiers: ModifierKeys) -> Option<String> {
    let code = code_name(key)?;
    let mut parts: Vec<&str> = Vec::new();
    if modifiers.contains(ModifierKeys::CONTROL) {
        parts.push("Control");
    }
    if modifiers.contains(ModifierKeys::ALT) {
        parts.push("Alt");
    }
    if modifiers.contains(ModifierKeys::SHIFT) {
        parts.push("Shift");
    }
    if modifiers.contains(ModifierKeys::WIN) {
        parts.push("Super");
    }
    parts.push(code);
    Some(parts.join("+"))
}

/// Human-readable display ("Ctrl + Alt + F8"), matching the legacy settings UI.
pub fn display_string(key: &str, modifiers: ModifierKeys) -> String {
    let mut parts: Vec<String> = Vec::new();
    if modifiers.contains(ModifierKeys::CONTROL) {
        parts.push("Ctrl".into());
    }
    if modifiers.contains(ModifierKeys::ALT) {
        parts.push("Alt".into());
    }
    if modifiers.contains(ModifierKeys::SHIFT) {
        parts.push("Shift".into());
    }
    if modifiers.contains(ModifierKeys::WIN) {
        parts.push("Win".into());
    }
    parts.push(key.trim().to_string());
    parts.join(" + ")
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn default_f8_maps_directly() {
        assert_eq!(shortcut_string("F8", ModifierKeys::NONE).unwrap(), "F8");
    }

    #[test]
    fn letters_and_modifiers() {
        let mods = ModifierKeys(ModifierKeys::CONTROL.0 | ModifierKeys::ALT.0);
        assert_eq!(shortcut_string("A", mods).unwrap(), "Control+Alt+KeyA");
    }

    #[test]
    fn digits_use_code_names() {
        assert_eq!(shortcut_string("D1", ModifierKeys::NONE).unwrap(), "Digit1");
    }

    #[test]
    fn display_formatting_matches_legacy() {
        let mods = ModifierKeys(ModifierKeys::CONTROL.0 | ModifierKeys::SHIFT.0);
        assert_eq!(display_string("F8", mods), "Ctrl + Shift + F8");
    }

    #[test]
    fn unknown_key_rejected() {
        assert!(shortcut_string("None", ModifierKeys::NONE).is_none());
    }
}
