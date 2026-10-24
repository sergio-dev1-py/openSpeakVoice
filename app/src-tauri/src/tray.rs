//! System tray with a monochrome lightning bolt icon that adapts to the OS
//! theme (white bolt on dark taskbars / menu bars, black on light; template
//! image on macOS so the system tints it automatically).

use crate::i18n;
use tauri::menu::{MenuBuilder, MenuItem};
use tauri::tray::{MouseButton, MouseButtonState, TrayIconBuilder, TrayIconEvent};
use tauri::{AppHandle, Manager};

const TRAY_WHITE: &[u8] = include_bytes!("../icons/tray-white.png");
const TRAY_BLACK: &[u8] = include_bytes!("../icons/tray-black.png");

#[cfg(windows)]
fn is_dark_theme() -> bool {
    use winreg::enums::HKEY_CURRENT_USER;
    use winreg::RegKey;
    RegKey::predef(HKEY_CURRENT_USER)
        .open_subkey(r"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")
        .and_then(|key| key.get_value::<u32, _>("AppsUseLightTheme"))
        .map(|light| light == 0)
        .unwrap_or(true)
}

#[cfg(not(windows))]
fn is_dark_theme() -> bool {
    true
}

pub fn setup(app: &AppHandle) -> Result<(), Box<dyn std::error::Error>> {
    let lang = app
        .try_state::<crate::state::AppManager>()
        .map(|m| m.settings.get().app_language)
        .unwrap_or_else(|| "es".to_string());

    let show_label = i18n::tr(&lang, "tray_show");
    let hide_label = i18n::tr(&lang, "tray_hide");
    let quit_label = i18n::tr(&lang, "tray_quit");

    let show_item = MenuItem::with_id(app, "show", show_label, true, None::<&str>)?;
    let hide_item = MenuItem::with_id(app, "hide", hide_label, true, None::<&str>)?;
    let quit_item = MenuItem::with_id(app, "quit", quit_label, true, None::<&str>)?;
    let menu = MenuBuilder::new(app)
        .item(&show_item)
        .item(&hide_item)
        .separator()
        .item(&quit_item)
        .build()?;

    let bytes = if cfg!(target_os = "macos") {
        TRAY_BLACK
    } else if is_dark_theme() {
        TRAY_WHITE
    } else {
        TRAY_BLACK
    };
    let icon = tauri::image::Image::from_bytes(bytes)?;

    #[allow(unused_mut)]
    let mut builder = TrayIconBuilder::with_id("main-tray")
        .icon(icon)
        .tooltip("VoiceToText")
        .menu(&menu)
        .show_menu_on_left_click(true)
        .on_menu_event(|app, event| match event.id.as_ref() {
            "show" => {
                if let Some(pill) = app.get_webview_window("pill") {
                    let _ = pill.show();
                }
            }
            "hide" => {
                if let Some(pill) = app.get_webview_window("pill") {
                    let _ = pill.hide();
                }
            }
            "quit" => app.exit(0),
            _ => {}
        })
        .on_tray_icon_event(|tray, event| {
            if let TrayIconEvent::Click {
                button: MouseButton::Left,
                button_state: MouseButtonState::Up,
                ..
            } = event
            {
                let app = tray.app_handle();
                if let Some(pill) = app.get_webview_window("pill") {
                    let _ = pill.show();
                    let _ = pill.set_focus();
                }
            }
        });

    #[cfg(target_os = "macos")]
    {
        builder = builder.icon_as_template(true);
    }

    builder.build(app)?;
    Ok(())
}
