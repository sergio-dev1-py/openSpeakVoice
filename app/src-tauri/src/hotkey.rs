//! Global hotkey registration. Settings store the hotkey in legacy C# `Keys`
//! names ("F8", "A", "D1", "Oem3"…); this maps them to the W3C `Code` names
//! understood by the `global-hotkey` crate.

use crate::settings::ModifierKeys;

/// C# Keys name → W3C KeyboardEvent.code name (as parsed by global-hotkey).
///
/// Matching is case-insensitive so the settings field accepts `F8`, `f8`,
/// `Space`, `SPACE`, `D1`, `PageUp`, raw punctuation, etc.
pub fn code_name(key: &str) -> Option<&'static str> {
    const FKEYS: [&str; 24] = [
        "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12", "F13", "F14",
        "F15", "F16", "F17", "F18", "F19", "F20", "F21", "F22", "F23", "F24",
    ];
    let k = key.trim();
    let upper = k.to_ascii_uppercase();

    // F-keys
    if let Some(rest) = upper.strip_prefix('F') {
        if let Ok(n) = rest.parse::<u32>() {
            if (1..=24).contains(&n) {
                return Some(FKEYS[(n - 1) as usize]);
            }
        }
    }

    // Letters and plain digits.
    if upper.len() == 1 {
        let c = upper.chars().next().unwrap();
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
        if c.is_ascii_digit() {
            return Some(match c {
                '0' => "Digit0",
                '1' => "Digit1",
                '2' => "Digit2",
                '3' => "Digit3",
                '4' => "Digit4",
                '5' => "Digit5",
                '6' => "Digit6",
                '7' => "Digit7",
                '8' => "Digit8",
                _ => "Digit9",
            });
        }
    }

    Some(match upper.as_str() {
        "D0" | "DIGIT0" => "Digit0",
        "D1" | "DIGIT1" => "Digit1",
        "D2" | "DIGIT2" => "Digit2",
        "D3" | "DIGIT3" => "Digit3",
        "D4" | "DIGIT4" => "Digit4",
        "D5" | "DIGIT5" => "Digit5",
        "D6" | "DIGIT6" => "Digit6",
        "D7" | "DIGIT7" => "Digit7",
        "D8" | "DIGIT8" => "Digit8",
        "D9" | "DIGIT9" => "Digit9",
        "NUMPAD0" | "NUM0" => "Numpad0",
        "NUMPAD1" | "NUM1" => "Numpad1",
        "NUMPAD2" | "NUM2" => "Numpad2",
        "NUMPAD3" | "NUM3" => "Numpad3",
        "NUMPAD4" | "NUM4" => "Numpad4",
        "NUMPAD5" | "NUM5" => "Numpad5",
        "NUMPAD6" | "NUM6" => "Numpad6",
        "NUMPAD7" | "NUM7" => "Numpad7",
        "NUMPAD8" | "NUM8" => "Numpad8",
        "NUMPAD9" | "NUM9" => "Numpad9",
        "SPACE" => "Space",
        "ENTER" | "RETURN" => "Enter",
        "ESCAPE" | "ESC" => "Escape",
        "TAB" => "Tab",
        "BACK" | "BACKSPACE" => "Backspace",
        "DELETE" | "DEL" => "Delete",
        "INSERT" | "INS" => "Insert",
        "HOME" => "Home",
        "END" => "End",
        "PRIOR" | "PAGEUP" | "PGUP" => "PageUp",
        "NEXT" | "PAGEDOWN" | "PGDN" => "PageDown",
        "UP" => "ArrowUp",
        "DOWN" => "ArrowDown",
        "LEFT" => "ArrowLeft",
        "RIGHT" => "ArrowRight",
        "CAPSLOCK" => "CapsLock",
        "NUMLOCK" => "NumLock",
        "SCROLLLOCK" => "ScrollLock",
        "PRINTSCREEN" | "PRTSC" => "PrintScreen",
        "PAUSE" => "Pause",
        "OEMTILDE" | "OEM3" | "BACKQUOTE" | "`" => "Backquote",
        "OEMMINUS" | "MINUS" | "-" => "Minus",
        "OEMPLUS" | "EQUAL" | "=" => "Equal",
        "OEMOPENBRACKETS" | "BRACKETLEFT" | "[" => "BracketLeft",
        "OEMCLOSEBRACKETS" | "BRACKETRIGHT" | "]" => "BracketRight",
        "OEMPIPE" | "BACKSLASH" | "\\" => "Backslash",
        "OEMSEMICOLON" | "SEMICOLON" | ";" => "Semicolon",
        "OEMQUOTES" | "QUOTE" | "'" => "Quote",
        "OEMCOMMA" | "COMMA" | "," => "Comma",
        "OEMPERIOD" | "PERIOD" | "." => "Period",
        "OEMQUESTION" | "SLASH" | "/" => "Slash",
        "MULTIPLY" | "NUMPADMULTIPLY" => "NumpadMultiply",
        "ADD" | "NUMPADADD" => "NumpadAdd",
        "SUBTRACT" | "NUMPADSUBTRACT" => "NumpadSubtract",
        "DIVIDE" | "NUMPADDIVIDE" => "NumpadDivide",
        "DECIMAL" | "NUMPADDECIMAL" => "NumpadDecimal",
        _ => return None,
    })
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

    #[test]
    fn named_keys_are_case_insensitive() {
        assert_eq!(
            shortcut_string("space", ModifierKeys::NONE).unwrap(),
            "Space"
        );
        assert_eq!(
            shortcut_string("SPACE", ModifierKeys::NONE).unwrap(),
            "Space"
        );
        assert_eq!(
            shortcut_string("pageup", ModifierKeys::NONE).unwrap(),
            "PageUp"
        );
        assert_eq!(
            shortcut_string("Esc", ModifierKeys::NONE).unwrap(),
            "Escape"
        );
    }

    #[test]
    fn punctuation_and_numpad_are_supported() {
        assert_eq!(shortcut_string("-", ModifierKeys::NONE).unwrap(), "Minus");
        assert_eq!(
            shortcut_string("numpad5", ModifierKeys::NONE).unwrap(),
            "Numpad5"
        );
    }
}
