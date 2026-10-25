//! Minimal Rust-side strings (tray menu, notifications). The full UI
//! localization lives in the TypeScript layer (src/shared/i18n.ts) with the
//! same es/en structure as the legacy WidgetStrings.

pub fn tr(lang: &str, key: &str) -> &'static str {
    let spanish = matches!(lang, "es" | "");
    match key {
        "tray_show" => {
            if spanish {
                "Mostrar"
            } else {
                "Show"
            }
        }
        "tray_hide" => {
            if spanish {
                "Ocultar"
            } else {
                "Hide"
            }
        }
        "tray_quit" => {
            if spanish {
                "Salir"
            } else {
                "Quit"
            }
        }
        _ => "",
    }
}
