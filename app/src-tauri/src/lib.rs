//! Slint application shell. The UI is a set of ordinary Slint windows; all
//! application behavior lives in `core::AppCore` and is polled from a single
//! low-frequency timer (30 fps only while recording).

pub mod audio;
pub mod core;
pub mod groq;
pub mod hotkey;
pub mod i18n;
pub mod inject;
pub mod languages;
pub mod logger;
pub mod settings;
pub mod whisper;

slint::include_modules!();

use crate::core::{AppCore, STATE_IDLE, STATE_LISTENING, STATE_TRANSCRIBING};
use crate::hotkey as hotkey_names;
use crate::languages::ALL_LANGUAGES;
use crate::settings::{AppSettings, ModifierKeys, SettingsService, SttProvider};
use global_hotkey::hotkey::HotKey;
use global_hotkey::{GlobalHotKeyEvent, GlobalHotKeyManager, HotKeyState};
use slint::{
    CloseRequestResponse, Color, ComponentHandle, Global, ModelRc, PhysicalPosition, SharedString,
    Timer, TimerMode, VecModel,
};
use std::cell::{Cell, RefCell};
use std::collections::hash_map::DefaultHasher;
use std::collections::VecDeque;
use std::hash::{Hash, Hasher};
use std::rc::Rc;
use std::sync::atomic::{AtomicBool, Ordering};
use std::sync::Arc;
use std::time::{Duration, Instant};

const WAVE_BARS: usize = 16;
const PILL_WIDTH: f32 = 121.0;
const PILL_HEIGHT: f32 = 32.0;
const REST_LEVEL: f32 = 0.06;
/// Static symmetric wave shown while idle (same silhouette as the legacy
/// widget, without any animation or repaint loop).
const REST_WAVE: [f32; WAVE_BARS] = [
    0.06, 0.10, 0.16, 0.23, 0.30, 0.37, 0.43, 0.48, 0.48, 0.43, 0.37, 0.30, 0.23, 0.16, 0.10, 0.06,
];
const IDLE_TICK_MS: u64 = 100;
const DRAG_TICK_MS: u64 = 16;
const RECORDING_TICK_MS: u64 = 33;
const POSITION_SAVE_DELAY: Duration = Duration::from_millis(500);
const ERROR_TOAST_MS: u64 = 5_000;
const INFO_TOAST_MS: u64 = 2_500;
const DELETE_CONFIRM_MS: u64 = 4_000;

pub fn run() -> Result<(), slint::PlatformError> {
    // The software renderer is the best fit for a tiny 2D overlay: it needs no
    // GPU/OpenGL context, keeps memory low and still provides per-pixel
    // transparency. Allow an explicit override for debugging.
    if std::env::var_os("SLINT_BACKEND").is_none() {
        std::env::set_var("SLINT_BACKEND", "winit-software");
    }
    logger::init(settings::app_data_dir());
    std::panic::set_hook(Box::new(|info| {
        log::error!("panic: {info}");
    }));

    let settings = SettingsService::new()
        .unwrap_or_else(|e| panic!("No se pudo inicializar la configuración: {e}"));
    log::info!(
        "VoiceToText starting (provider {:?}, theme {}, model {})",
        settings.get().active_provider,
        settings.get().theme,
        settings.get().local_model_name
    );

    let core = AppCore::new(settings);
    let pill = PillWindow::new()?;
    let tray = TrayIcon::new()
        .map_err(|e| {
            log::warn!("system tray unavailable: {e}");
            e
        })
        .ok();
    if tray.is_some() {
        log::info!("system tray initialized");
    }

    let ui = AppUi::new(core, pill, tray);
    ui.install_callbacks();
    ui.restore_position_and_show();
    ui.apply_visuals();
    ui.sync_pill_state();
    ui.reregister_hotkey();
    ui.start_timer();

    slint::run_event_loop()
}

// ---------------------------------------------------------------------------
// App state / UI glue
// ---------------------------------------------------------------------------

#[derive(Clone)]
struct PillVisual {
    dark: bool,
    is_groq: bool,
    is_translation: bool,
    target_code: String,
    target_label: String,
    provider_label: String,
    mode_label: String,
    rgb: (u8, u8, u8),
    opacity: f32,
    multicolor: bool,
}

impl PillVisual {
    fn from_settings(s: &AppSettings) -> Self {
        let dark = s.theme != "light";
        let mut rgb = parse_color(&s.border_color).unwrap_or((255, 255, 255));
        // A white wave is invisible on the light pill; the old UI used blue.
        if !dark && (rgb.0 as u32 + rgb.1 as u32 + rgb.2 as u32) / 3 > 235 {
            rgb = (37, 99, 235);
        }
        let target_label = languages::display_name(&s.target_language)
            .map(str::to_string)
            .unwrap_or_else(|| s.target_language.to_uppercase());
        Self {
            dark,
            is_groq: s.active_provider == SttProvider::GroqApi,
            is_translation: s.mode == "translation",
            target_code: s.target_language.clone(),
            target_label,
            provider_label: if s.active_provider == SttProvider::GroqApi {
                "Groq".into()
            } else {
                "Whisper".into()
            },
            mode_label: if s.mode == "translation" {
                "Traducción".into()
            } else {
                "Transcripción".into()
            },
            rgb,
            opacity: s.border_opacity as f32 / 255.0,
            multicolor: s.multicolor_border,
        }
    }
}

struct AppUi {
    core: Arc<AppCore>,
    pill: PillWindow,
    tray: Option<TrayIcon>,
    management: RefCell<Option<ManagementWindow>>,
    menu: RefCell<Option<MenuWindow>>,
    menu_opened_at: Cell<Option<Instant>>,
    menu_mouse_down: Cell<bool>,
    toast: RefCell<Option<ToastWindow>>,
    hotkey_manager: RefCell<Option<GlobalHotKeyManager>>,
    current_hotkey: RefCell<Option<HotKey>>,
    visual: RefCell<PillVisual>,
    levels: RefCell<Vec<f32>>,
    hue: Cell<f32>,
    last_state: Cell<u8>,
    pill_chrome_ready: Cell<bool>,
    drag_active: Arc<AtomicBool>,
    drag_was_active: Cell<bool>,
    last_seen_pos: Cell<(i32, i32)>,
    saved_pos: Cell<(i32, i32)>,
    pending_pos_save: Cell<Option<Instant>>,
    pending_messages: RefCell<VecDeque<(bool, String)>>,
    toast_hide_at: Cell<Option<Instant>>,
    pending_delete: RefCell<Option<(String, Instant)>>,
    last_model_rows_hash: Cell<u64>,
    last_usage_text: RefCell<(String, String)>,
    timer: Timer,
    tick_ms: Cell<u64>,
    quitting: Cell<bool>,
}

impl AppUi {
    fn new(core: Arc<AppCore>, pill: PillWindow, tray: Option<TrayIcon>) -> Rc<Self> {
        let settings = core.settings.get();
        let visual = PillVisual::from_settings(&settings);
        let pos = (settings.widget_pos_x, settings.widget_pos_y);
        Rc::new(Self {
            core,
            pill,
            tray,
            management: RefCell::new(None),
            menu: RefCell::new(None),
            menu_opened_at: Cell::new(None),
            menu_mouse_down: Cell::new(false),
            toast: RefCell::new(None),
            hotkey_manager: RefCell::new(None),
            current_hotkey: RefCell::new(None),
            visual: RefCell::new(visual),
            levels: RefCell::new(REST_WAVE.to_vec()),
            hue: Cell::new(0.0),
            last_state: Cell::new(STATE_IDLE),
            pill_chrome_ready: Cell::new(false),
            drag_active: Arc::new(AtomicBool::new(false)),
            drag_was_active: Cell::new(false),
            last_seen_pos: Cell::new(pos),
            saved_pos: Cell::new(pos),
            pending_pos_save: Cell::new(None),
            pending_messages: RefCell::new(VecDeque::new()),
            toast_hide_at: Cell::new(None),
            pending_delete: RefCell::new(None),
            last_model_rows_hash: Cell::new(u64::MAX),
            last_usage_text: RefCell::new((String::new(), String::new())),
            timer: Timer::default(),
            tick_ms: Cell::new(0),
            quitting: Cell::new(false),
        })
    }

    fn install_callbacks(self: &Rc<Self>) {
        {
            let weak = Rc::downgrade(self);
            self.pill.on_toggle(move || {
                if let Some(ui) = weak.upgrade() {
                    ui.core.toggle_recording();
                    ui.sync_pill_state();
                    ui.pump_messages();
                }
            });
        }
        {
            let weak = Rc::downgrade(self);
            self.pill.on_open_settings(move || {
                if let Some(ui) = weak.upgrade() {
                    ui.open_management(0);
                }
            });
        }
        {
            let weak = Rc::downgrade(self);
            self.pill.on_open_keys(move || {
                if let Some(ui) = weak.upgrade() {
                    ui.open_management(1);
                }
            });
        }
        {
            let weak = Rc::downgrade(self);
            self.pill.on_open_models(move || {
                if let Some(ui) = weak.upgrade() {
                    ui.open_management(2);
                }
            });
        }
        {
            let weak = Rc::downgrade(self);
            self.pill.on_set_provider(move |provider| {
                if let Some(ui) = weak.upgrade() {
                    ui.core.set_provider(provider.to_string());
                    ui.apply_visuals();
                    ui.sync_pill_state();
                }
            });
        }
        {
            let weak = Rc::downgrade(self);
            self.pill.on_set_mode(move |mode| {
                if let Some(ui) = weak.upgrade() {
                    ui.core.set_mode(mode.to_string());
                    ui.apply_visuals();
                    ui.sync_pill_state();
                }
            });
        }
        {
            let weak = Rc::downgrade(self);
            self.pill.on_set_target(move |code| {
                if let Some(ui) = weak.upgrade() {
                    ui.core.set_target_language(code.to_string());
                    ui.apply_visuals();
                    ui.sync_pill_state();
                }
            });
        }
        {
            let weak = Rc::downgrade(self);
            self.pill.on_hide_pill(move || {
                if let Some(ui) = weak.upgrade() {
                    ui.hide_pill();
                }
            });
        }
        {
            let weak = Rc::downgrade(self);
            self.pill.on_quit_app(move || {
                if let Some(ui) = weak.upgrade() {
                    ui.quit();
                }
            });
        }
        {
            let weak = Rc::downgrade(self);
            self.pill.on_begin_drag(move || {
                if let Some(ui) = weak.upgrade() {
                    ui.begin_drag();
                }
            });
        }
        {
            let weak = Rc::downgrade(self);
            self.pill.on_request_menu(move |x, y| {
                if let Some(ui) = weak.upgrade() {
                    ui.request_menu(x, y);
                }
            });
        }

        {
            let weak = Rc::downgrade(self);
            self.pill.window().on_close_requested(move || {
                if let Some(ui) = weak.upgrade() {
                    ui.hide_pill();
                }
                CloseRequestResponse::HideWindow
            });
        }

        if let Some(tray) = &self.tray {
            {
                let weak = Rc::downgrade(self);
                tray.on_show_pill(move || {
                    if let Some(ui) = weak.upgrade() {
                        ui.show_pill();
                    }
                });
            }
            {
                let weak = Rc::downgrade(self);
                tray.on_hide_pill(move || {
                    if let Some(ui) = weak.upgrade() {
                        ui.hide_pill();
                    }
                });
            }
            {
                let weak = Rc::downgrade(self);
                tray.on_quit_app(move || {
                    if let Some(ui) = weak.upgrade() {
                        ui.quit();
                    }
                });
            }
        }
    }

    fn restore_position_and_show(&self) {
        let settings = self.core.settings.get();
        let pos = PhysicalPosition::new(settings.widget_pos_x, settings.widget_pos_y);
        if let Err(e) = self.pill.show() {
            log::error!("failed to show pill: {e}");
            return;
        }
        self.pill.window().set_position(pos);
        apply_tool_window(self.pill.window(), true, true, true);
        let actual = self.pill.window().position();
        self.last_seen_pos.set((actual.x, actual.y));
        self.saved_pos.set((actual.x, actual.y));
    }

    fn start_timer(self: &Rc<Self>) {
        let weak = Rc::downgrade(self);
        self.tick_ms.set(IDLE_TICK_MS);
        self.timer.start(
            TimerMode::Repeated,
            Duration::from_millis(IDLE_TICK_MS),
            move || {
                if let Some(ui) = weak.upgrade() {
                    ui.tick();
                }
            },
        );
    }

    fn tick(self: &Rc<Self>) {
        if self.quitting.get() {
            return;
        }
        self.ensure_pill_chrome();
        self.core.stop_if_too_long();
        self.poll_hotkey();
        self.check_menu_outside_click();
        self.pump_messages();
        self.sync_pill_state();
        self.update_wave();
        self.update_position_save();
        self.update_toast();

        // The drag worker moves the native window directly. Slint's software
        // surface must be repainted afterwards, otherwise the transparent
        // pixels reveal whatever is behind the pill (a browser tab, usually).
        let dragging = self.drag_active.load(Ordering::Relaxed);
        if dragging || self.drag_was_active.get() {
            // Force a full-surface repaint; request_redraw alone only asks for
            // a frame, while the software renderer may have no damage region.
            self.pill.set_repaint_flash(!self.pill.get_repaint_flash());
            self.pill.window().request_redraw();
        }
        self.drag_was_active.set(dragging);

        let wanted = if dragging {
            DRAG_TICK_MS
        } else if self.core.is_listening() || self.menu.borrow().is_some() {
            RECORDING_TICK_MS
        } else {
            IDLE_TICK_MS
        };
        if wanted != self.tick_ms.get() {
            self.tick_ms.set(wanted);
            self.timer.set_interval(Duration::from_millis(wanted));
        }
    }

    // ——— pill ———

    fn apply_visuals(&self) {
        let settings = self.core.settings.get();
        let visual = PillVisual::from_settings(&settings);
        self.pill.set_dark(visual.dark);
        self.pill.set_is_groq(visual.is_groq);
        self.pill.set_is_translation(visual.is_translation);
        self.pill
            .set_target_code(visual.target_code.as_str().into());
        self.pill
            .set_target_label(visual.target_label.as_str().into());
        self.pill
            .set_provider_label(visual.provider_label.as_str().into());
        self.pill.set_mode_label(visual.mode_label.as_str().into());
        self.pill.set_accent(base_accent(&visual));
        self.pill
            .set_wave_color(self.wave_color(&visual, self.core.is_listening()));

        if let Some(tray) = &self.tray {
            tray.set_dark(visual.dark);
            tray.set_show_label(i18n::tr(&settings.app_language, "tray_show").into());
            tray.set_hide_label(i18n::tr(&settings.app_language, "tray_hide").into());
            tray.set_quit_label(i18n::tr(&settings.app_language, "tray_quit").into());
        }
        if let Some(toast) = self.toast.borrow().as_ref() {
            toast.set_dark(visual.dark);
        }
        if let Some(win) = self.management.borrow().as_ref() {
            win.set_dark(visual.dark);
            apply_management_palette(win, visual.dark);
        }
        *self.visual.borrow_mut() = visual;
    }

    fn wave_color(&self, visual: &PillVisual, listening: bool) -> Color {
        if visual.multicolor {
            let (r, g, b) = hsv_to_rgb(self.hue.get(), 0.9, 0.65);
            Color::from_argb_u8(242, r, g, b)
        } else {
            let alpha = if listening {
                0.68 + 0.32 * visual.opacity
            } else {
                0.26 + 0.18 * visual.opacity
            };
            Color::from_argb_u8(
                (alpha * 255.0).round().clamp(0.0, 255.0) as u8,
                visual.rgb.0,
                visual.rgb.1,
                visual.rgb.2,
            )
        }
    }

    fn sync_pill_state(&self) {
        let state = self.core.state();
        let listening = state == STATE_LISTENING;
        let transcribing = state == STATE_TRANSCRIBING;
        if self.pill.get_listening() != listening {
            self.pill.set_listening(listening);
        }
        if self.pill.get_transcribing() != transcribing {
            self.pill.set_transcribing(transcribing);
        }
        let can_hide = state == STATE_IDLE;
        if self.pill.get_can_hide() != can_hide {
            self.pill.set_can_hide(can_hide);
        }
        if state != self.last_state.get() {
            self.last_state.set(state);
            if listening {
                *self.levels.borrow_mut() = REST_WAVE.to_vec();
                self.push_levels();
            } else {
                self.reset_levels();
                let visual = self.visual.borrow().clone();
                let color = self.wave_color(&visual, false);
                if self.pill.get_wave_color() != color {
                    self.pill.set_wave_color(color);
                }
            }
        }
    }

    fn update_wave(&self) {
        if !self.core.is_listening() || !self.pill.window().is_visible() {
            return;
        }
        let visual = self.visual.borrow().clone();
        let raw = self.core.audio_level().clamp(0.0, 1.0);
        let target = (raw.powf(0.7) * 1.2).clamp(0.0, 1.0);
        let mut levels = self.levels.borrow_mut();
        let previous = *levels.last().unwrap_or(&REST_LEVEL);
        let smoothed = previous + (target - previous) * if target > previous { 0.7 } else { 0.3 };
        levels.rotate_left(1);
        if let Some(last) = levels.last_mut() {
            *last = smoothed.max(REST_LEVEL);
        }
        let snapshot = levels.clone();
        drop(levels);
        self.pill.set_levels(ModelRc::new(VecModel::from(snapshot)));

        let mut hue = self.hue.get();
        if visual.multicolor {
            hue = (hue + 1.2) % 360.0;
            self.hue.set(hue);
        }
        let color = self.wave_color(&visual, true);
        if self.pill.get_wave_color() != color {
            self.pill.set_wave_color(color);
        }
    }

    fn reset_levels(&self) {
        let mut levels = self.levels.borrow_mut();
        if levels.len() == REST_WAVE.len()
            && levels
                .iter()
                .zip(REST_WAVE.iter())
                .all(|(current, rest)| (*current - *rest).abs() < f32::EPSILON)
        {
            return;
        }
        *levels = REST_WAVE.to_vec();
        let snapshot = levels.clone();
        drop(levels);
        self.pill.set_levels(ModelRc::new(VecModel::from(snapshot)));
    }

    fn push_levels(&self) {
        let snapshot = self.levels.borrow().clone();
        self.pill.set_levels(ModelRc::new(VecModel::from(snapshot)));
    }

    fn show_pill(&self) {
        self.close_menu();
        if self.pill.show().is_ok() {
            // The Win32 handle may not be available immediately after show();
            // the first UI tick retries until the overlay chrome is applied.
            self.pill_chrome_ready
                .set(apply_tool_window(self.pill.window(), true, true, true));
        }
    }

    fn ensure_pill_chrome(&self) {
        if self.pill_chrome_ready.get() {
            return;
        }
        if apply_tool_window(self.pill.window(), true, true, true) {
            self.pill_chrome_ready.set(true);
        }
    }

    fn hide_pill(&self) {
        self.close_menu();
        self.save_position_now();
        let _ = self.pill.hide();
    }

    fn update_position_save(&self) {
        let pos = self.pill.window().position();
        let pos = (pos.x, pos.y);
        if pos != self.last_seen_pos.get() {
            self.last_seen_pos.set(pos);
            self.pending_pos_save
                .set(Some(Instant::now() + POSITION_SAVE_DELAY));
        }
        if let Some(deadline) = self.pending_pos_save.get() {
            if Instant::now() >= deadline {
                self.pending_pos_save.set(None);
                if pos != self.saved_pos.get() {
                    self.save_position_now_at(pos);
                }
            }
        }
    }

    fn save_position_now(&self) {
        let pos = self.pill.window().position();
        let pos = (pos.x, pos.y);
        if pos != self.saved_pos.get() {
            self.save_position_now_at(pos);
        }
    }

    fn save_position_now_at(&self, pos: (i32, i32)) {
        self.core.settings.update(|s| {
            s.widget_pos_x = pos.0;
            s.widget_pos_y = pos.1;
        });
        self.saved_pos.set(pos);
        self.last_seen_pos.set(pos);
    }

    /// Starts the native drag worker. The window is non-activating, so normal
    /// pointer moves are not reliable; follow the OS cursor instead.
    #[cfg(windows)]
    fn begin_drag(&self) {
        self.close_menu();
        use windows_sys::Win32::Foundation::{POINT, RECT};
        use windows_sys::Win32::UI::Input::KeyboardAndMouse::{GetAsyncKeyState, VK_LBUTTON};
        use windows_sys::Win32::UI::WindowsAndMessaging::{
            GetCursorPos, GetWindowRect, SetWindowPos, HWND_TOPMOST, SWP_NOACTIVATE, SWP_NOSIZE,
        };

        let Some(hwnd) = hwnd_of(self.pill.window()) else {
            return;
        };
        let core = Arc::clone(&self.core);
        let drag_active = Arc::clone(&self.drag_active);
        self.tick_ms.set(DRAG_TICK_MS);
        self.timer.set_interval(Duration::from_millis(DRAG_TICK_MS));
        self.pill.window().request_redraw();
        let _ = std::thread::Builder::new()
            .name("pill-drag".into())
            .spawn(move || {
                drag_active.store(true, Ordering::SeqCst);
                let hwnd = hwnd as _;
                unsafe {
                    let mut cursor_start = POINT { x: 0, y: 0 };
                    let mut rect: RECT = std::mem::zeroed();
                    if GetCursorPos(&mut cursor_start) == 0 || GetWindowRect(hwnd, &mut rect) == 0 {
                        drag_active.store(false, Ordering::SeqCst);
                        return;
                    }
                    let start_cursor = (cursor_start.x, cursor_start.y);
                    let start_window = (rect.left, rect.top);
                    let started = Instant::now();
                    let mut moving = false;

                    loop {
                        if started.elapsed() > Duration::from_secs(120) {
                            break;
                        }
                        let down = (GetAsyncKeyState(VK_LBUTTON as i32) as u16 & 0x8000) != 0;
                        if !down {
                            break;
                        }
                        let mut cursor = POINT { x: 0, y: 0 };
                        if GetCursorPos(&mut cursor) != 0 {
                            let dx = cursor.x - start_cursor.0;
                            let dy = cursor.y - start_cursor.1;
                            if !moving && (dx.abs() > 3 || dy.abs() > 3) {
                                moving = true;
                            }
                            if moving {
                                SetWindowPos(
                                    hwnd,
                                    HWND_TOPMOST,
                                    start_window.0 + dx,
                                    start_window.1 + dy,
                                    0,
                                    0,
                                    SWP_NOSIZE | SWP_NOACTIVATE,
                                );
                            }
                        }
                        std::thread::sleep(Duration::from_millis(8));
                    }

                    if moving {
                        let mut final_rect: RECT = std::mem::zeroed();
                        if GetWindowRect(hwnd, &mut final_rect) != 0 {
                            core.settings.update(|s| {
                                s.widget_pos_x = final_rect.left;
                                s.widget_pos_y = final_rect.top;
                            });
                        }
                    }
                    drag_active.store(false, Ordering::SeqCst);
                }
            });
    }

    #[cfg(not(windows))]
    fn begin_drag(&self) {
        // WindowMoveArea is not used because it consumes right clicks. Keep the
        // widget usable on other platforms; native dragging is Windows-only.
    }

    /// Opens the context menu after the triggering release has been fully
    /// processed, so the same click can't activate the item under the cursor.
    fn request_menu(self: &Rc<Self>, x: f32, y: f32) {
        let weak = Rc::downgrade(self);
        Timer::single_shot(Duration::from_millis(50), move || {
            if let Some(ui) = weak.upgrade() {
                if ui.pill.window().is_visible() {
                    ui.open_menu_at(x, y);
                }
            }
        });
    }

    // ——— popup menu ———

    fn open_menu_at(self: &Rc<Self>, _x: f32, _y: f32) {
        self.close_menu();
        let menu = match MenuWindow::new() {
            Ok(menu) => menu,
            Err(e) => {
                log::warn!("failed to create menu window: {e}");
                return;
            }
        };
        let visual = self.visual.borrow().clone();
        menu.set_dark(visual.dark);
        menu.set_is_groq(visual.is_groq);
        menu.set_is_translation(visual.is_translation);
        menu.set_target_code(visual.target_code.as_str().into());
        menu.set_target_label(visual.target_label.as_str().into());
        menu.set_provider_label(visual.provider_label.as_str().into());
        menu.set_mode_label(visual.mode_label.as_str().into());
        menu.set_can_hide(self.core.state() == STATE_IDLE);

        self.setup_menu_window(&menu);
        *self.menu.borrow_mut() = Some(menu.clone_strong());
        if let Err(e) = menu.show() {
            log::warn!("failed to show menu window: {e}");
            self.menu.borrow_mut().take();
            return;
        }
        apply_tool_window(menu.window(), false, true, false);
        position_menu(menu.window(), self.pill.window());
        focus_window(menu.window());
        menu.invoke_focus_menu();
        self.menu_opened_at.set(Some(Instant::now()));

        // The native window is realized by the event loop slightly later.
        let weak = Rc::downgrade(self);
        Timer::single_shot(Duration::from_millis(30), move || {
            if let Some(ui) = weak.upgrade() {
                ui.position_open_menu();
            }
        });
    }

    fn position_open_menu(&self) {
        let Some(menu) = self.menu.borrow().as_ref().map(|menu| menu.clone_strong()) else {
            return;
        };
        if !menu.window().is_visible() {
            return;
        }
        // The handle is available now; retry the overlay chrome that could not
        // be applied immediately after show().
        let _ = apply_tool_window(menu.window(), false, true, false);
        position_menu(menu.window(), self.pill.window());
        focus_window(menu.window());
        menu.invoke_focus_menu();
    }

    fn setup_menu_window(self: &Rc<Self>, menu: &MenuWindow) {
        let weak = Rc::downgrade(self);
        menu.on_close_menu(move || {
            if let Some(ui) = weak.upgrade() {
                ui.close_menu();
            }
        });
        let weak = Rc::downgrade(self);
        menu.on_open_settings(move || {
            if let Some(ui) = weak.upgrade() {
                ui.close_menu();
                ui.open_management(0);
            }
        });
        let weak = Rc::downgrade(self);
        menu.on_open_keys(move || {
            if let Some(ui) = weak.upgrade() {
                ui.close_menu();
                ui.open_management(1);
            }
        });
        let weak = Rc::downgrade(self);
        menu.on_open_models(move || {
            if let Some(ui) = weak.upgrade() {
                ui.close_menu();
                ui.open_management(2);
            }
        });
        let weak = Rc::downgrade(self);
        menu.on_set_provider(move |provider| {
            if let Some(ui) = weak.upgrade() {
                ui.core.set_provider(provider.to_string());
                ui.apply_visuals();
                ui.sync_pill_state();
                ui.close_menu();
            }
        });
        let weak = Rc::downgrade(self);
        menu.on_set_mode(move |mode| {
            if let Some(ui) = weak.upgrade() {
                ui.core.set_mode(mode.to_string());
                ui.apply_visuals();
                ui.sync_pill_state();
                ui.close_menu();
            }
        });
        let weak = Rc::downgrade(self);
        menu.on_set_target(move |target| {
            if let Some(ui) = weak.upgrade() {
                ui.core.set_target_language(target.to_string());
                ui.apply_visuals();
                ui.sync_pill_state();
                ui.close_menu();
            }
        });
        let weak = Rc::downgrade(self);
        menu.on_hide_pill(move || {
            if let Some(ui) = weak.upgrade() {
                ui.close_menu();
                ui.hide_pill();
            }
        });
        let weak = Rc::downgrade(self);
        menu.on_quit_app(move || {
            if let Some(ui) = weak.upgrade() {
                ui.close_menu();
                ui.quit();
            }
        });
        let weak = Rc::downgrade(self);
        menu.window().on_close_requested(move || {
            if let Some(ui) = weak.upgrade() {
                ui.close_menu();
            }
            CloseRequestResponse::HideWindow
        });
    }

    fn close_menu(&self) {
        self.menu_opened_at.set(None);
        self.menu_mouse_down.set(false);
        let menu = self.menu.borrow_mut().take();
        if let Some(menu) = menu {
            Timer::single_shot(Duration::ZERO, move || {
                let _ = menu.hide();
            });
        }
    }

    fn check_menu_outside_click(&self) {
        let Some(opened) = self.menu_opened_at.get() else {
            return;
        };
        if opened.elapsed() > Duration::from_secs(20) {
            self.close_menu();
            return;
        }
        let Some(menu) = self.menu.borrow().as_ref().map(|menu| menu.clone_strong()) else {
            self.menu_opened_at.set(None);
            return;
        };
        if !menu.window().is_visible() {
            self.menu_opened_at.set(None);
            return;
        }
        #[cfg(windows)]
        {
            use windows_sys::Win32::Foundation::{POINT, RECT};
            use windows_sys::Win32::UI::Input::KeyboardAndMouse::{
                GetAsyncKeyState, VK_LBUTTON, VK_RBUTTON,
            };
            use windows_sys::Win32::UI::WindowsAndMessaging::{GetCursorPos, GetWindowRect};

            if opened.elapsed() > Duration::from_millis(300) {
                if let (Some(menu_hwnd), Some(foreground)) =
                    (hwnd_of(menu.window()), foreground_window())
                {
                    if menu_hwnd != foreground {
                        self.close_menu();
                        return;
                    }
                }
            }

            let left = unsafe { GetAsyncKeyState(VK_LBUTTON as i32) as u16 & 0x8000 != 0 };
            let right = unsafe { GetAsyncKeyState(VK_RBUTTON as i32) as u16 & 0x8000 != 0 };
            let down = left || right;
            let was_down = self.menu_mouse_down.replace(down);
            if down && !was_down {
                let Some(menu_hwnd) = hwnd_of(menu.window()) else {
                    return;
                };
                let mut cursor = POINT { x: 0, y: 0 };
                let mut rect: RECT = unsafe { std::mem::zeroed() };
                if unsafe { GetCursorPos(&mut cursor) } == 0
                    || unsafe { GetWindowRect(menu_hwnd as _, &mut rect) } == 0
                {
                    return;
                }
                let inside = cursor.x >= rect.left
                    && cursor.x < rect.right
                    && cursor.y >= rect.top
                    && cursor.y < rect.bottom;
                if !inside {
                    self.close_menu();
                }
            }
        }
    }

    // ——— hotkey ———

    fn reregister_hotkey(&self) {
        if let Err(e) = self.register_current_hotkey() {
            self.core
                .error(format!("No se pudo registrar el atajo: {e}"));
        }
    }

    fn register_hotkey(&self, hotkey: HotKey) -> Result<(), String> {
        let mut slot = self.hotkey_manager.borrow_mut();
        if slot.is_none() {
            match GlobalHotKeyManager::new() {
                Ok(manager) => *slot = Some(manager),
                Err(e) => return Err(format!("No se pudo inicializar el gestor de atajos: {e}")),
            }
        }
        slot.as_ref()
            .expect("manager initialized")
            .register(hotkey)
            .map_err(|e| e.to_string())
    }

    fn unregister_hotkey(&self, hotkey: HotKey) {
        if let Some(manager) = self.hotkey_manager.borrow().as_ref() {
            let _ = manager.unregister(hotkey);
        }
    }

    fn register_current_hotkey(&self) -> Result<(), String> {
        let settings = self.core.settings.get();
        let combo = hotkey_names::shortcut_string(&settings.hot_key, settings.modifiers)
            .unwrap_or_else(|| "F8".to_string());
        let hotkey: HotKey = combo
            .parse()
            .map_err(|e| format!("atajo inválido ({combo}): {e}"))?;
        if let Some(old) = self.current_hotkey.borrow_mut().take() {
            self.unregister_hotkey(old);
        }
        self.register_hotkey(hotkey)?;
        log::info!("hotkey registered: {combo}");
        *self.current_hotkey.borrow_mut() = Some(hotkey);
        Ok(())
    }

    fn poll_hotkey(&self) {
        let Some(current) = *self.current_hotkey.borrow() else {
            return;
        };
        while let Ok(event) = GlobalHotKeyEvent::receiver().try_recv() {
            if event.id() == current.id() && event.state() == HotKeyState::Pressed {
                log::info!("global hotkey pressed");
                self.close_menu();
                if !self.pill.window().is_visible() {
                    self.show_pill();
                } else {
                    self.core.toggle_recording();
                    self.sync_pill_state();
                    self.pump_messages();
                }
            }
        }
    }

    // ——— messages / toast ———

    fn pump_messages(&self) {
        while let Some(message) = self.core.take_message() {
            self.pending_messages.borrow_mut().push_back(message);
        }
    }

    fn update_toast(self: &Rc<Self>) {
        if let Some(deadline) = self.toast_hide_at.get() {
            if Instant::now() >= deadline {
                self.close_toast();
            }
        }
        if self.toast_hide_at.get().is_none() {
            let message = self.pending_messages.borrow_mut().pop_front();
            if let Some((is_error, message)) = message {
                self.show_toast(is_error, message);
            }
        }
    }

    fn show_toast(self: &Rc<Self>, is_error: bool, message: String) {
        let existing = self
            .toast
            .borrow()
            .as_ref()
            .map(|toast| toast.clone_strong());
        let toast = if let Some(toast) = existing {
            toast
        } else {
            let toast = match ToastWindow::new() {
                Ok(toast) => toast,
                Err(e) => {
                    log::warn!("failed to create toast window: {e}");
                    return;
                }
            };
            self.setup_toast_window(&toast);
            *self.toast.borrow_mut() = Some(toast.clone_strong());
            toast
        };
        let dark = self.visual.borrow().dark;
        toast.set_dark(dark);
        toast.set_is_error(is_error);
        toast.set_message(message.as_str().into());
        if let Err(e) = toast.show() {
            log::warn!("failed to show toast: {e}");
            return;
        }
        apply_tool_window(toast.window(), true, true, false);
        position_toast(toast.window(), self.pill.window());
        let ms = if is_error {
            ERROR_TOAST_MS
        } else {
            INFO_TOAST_MS
        };
        self.toast_hide_at
            .set(Some(Instant::now() + Duration::from_millis(ms)));

        let weak = Rc::downgrade(self);
        Timer::single_shot(Duration::from_millis(20), move || {
            if let Some(ui) = weak.upgrade() {
                ui.position_open_toast();
            }
        });
    }

    fn position_open_toast(self: &Rc<Self>) {
        let Some(toast) = self
            .toast
            .borrow()
            .as_ref()
            .map(|toast| toast.clone_strong())
        else {
            return;
        };
        if toast.window().is_visible() {
            let _ = apply_tool_window(toast.window(), true, true, false);
            position_toast(toast.window(), self.pill.window());
        }
    }

    fn setup_toast_window(self: &Rc<Self>, toast: &ToastWindow) {
        let weak = Rc::downgrade(self);
        toast.on_close_toast(move || {
            if let Some(ui) = weak.upgrade() {
                ui.close_toast();
            }
        });
        let weak = Rc::downgrade(self);
        toast.window().on_close_requested(move || {
            if let Some(ui) = weak.upgrade() {
                ui.close_toast();
            }
            CloseRequestResponse::HideWindow
        });
    }

    fn close_toast(&self) {
        self.toast_hide_at.set(None);
        let toast = self.toast.borrow_mut().take();
        if let Some(toast) = toast {
            Timer::single_shot(Duration::ZERO, move || {
                let _ = toast.hide();
            });
        }
    }

    // ——— management window ———

    fn open_management(self: &Rc<Self>, view: i32) {
        self.close_menu();
        if let Some(win) = self
            .management
            .borrow()
            .as_ref()
            .map(|win| win.clone_strong())
        {
            self.populate_management(&win, view);
            apply_management_palette(&win, self.visual.borrow().dark);
            if win.show().is_ok() {
                apply_tool_window(win.window(), false, false, false);
                center_window(win.window());
                focus_window(win.window());
            }
            return;
        }

        let win = match ManagementWindow::new() {
            Ok(win) => win,
            Err(e) => {
                log::warn!("failed to create management window: {e}");
                return;
            }
        };
        self.setup_management_window(&win);
        *self.management.borrow_mut() = Some(win.clone_strong());
        self.populate_management(&win, view);
        apply_management_palette(&win, self.visual.borrow().dark);
        if let Err(e) = win.show() {
            log::warn!("failed to show management window: {e}");
            self.management.borrow_mut().take();
            return;
        }
        apply_tool_window(win.window(), false, false, false);
        center_window(win.window());
        focus_window(win.window());
        self.defer_management_position();
    }

    fn defer_management_position(self: &Rc<Self>) {
        let weak = Rc::downgrade(self);
        Timer::single_shot(Duration::from_millis(30), move || {
            if let Some(ui) = weak.upgrade() {
                if let Some(win) = ui
                    .management
                    .borrow()
                    .as_ref()
                    .map(|win| win.clone_strong())
                {
                    if win.window().is_visible() {
                        let _ = apply_tool_window(win.window(), false, false, false);
                        center_window(win.window());
                        focus_window(win.window());
                    }
                }
            }
        });
    }

    fn setup_management_window(self: &Rc<Self>, win: &ManagementWindow) {
        let weak = Rc::downgrade(self);
        win.on_close_window(move || {
            if let Some(ui) = weak.upgrade() {
                ui.close_management();
            }
        });
        let weak = Rc::downgrade(self);
        win.on_tick(move || {
            if let Some(ui) = weak.upgrade() {
                ui.refresh_management();
            }
        });
        let weak = Rc::downgrade(self);
        win.on_save_settings(move || {
            if let Some(ui) = weak.upgrade() {
                ui.save_settings();
            }
        });
        let weak = Rc::downgrade(self);
        win.on_save_keys(move || {
            if let Some(ui) = weak.upgrade() {
                ui.save_keys();
            }
        });
        let weak = Rc::downgrade(self);
        win.on_model_action(move |action, name| {
            if let Some(ui) = weak.upgrade() {
                ui.model_action(action.as_str(), name.as_str());
            }
        });
        let weak = Rc::downgrade(self);
        win.on_gpu_changed(move |checked| {
            if let Some(ui) = weak.upgrade() {
                ui.set_gpu(checked);
            }
        });
        let weak = Rc::downgrade(self);
        win.window().on_close_requested(move || {
            if let Some(ui) = weak.upgrade() {
                ui.close_management();
            }
            CloseRequestResponse::HideWindow
        });
    }

    fn close_management(&self) {
        let win = self.management.borrow_mut().take();
        if let Some(win) = win {
            Timer::single_shot(Duration::ZERO, move || {
                let _ = win.hide();
            });
        }
    }

    fn populate_management(&self, win: &ManagementWindow, view: i32) {
        self.last_model_rows_hash.set(u64::MAX);
        *self.last_usage_text.borrow_mut() = (String::new(), String::new());
        let settings = self.core.settings.get();
        let names: Vec<SharedString> = ALL_LANGUAGES
            .iter()
            .map(|lg| format!("{} ({})", lg.name, lg.code).into())
            .collect();
        win.set_language_names(ModelRc::new(VecModel::from(names)));
        win.set_language_index(index_of_language(&settings.app_language));
        win.set_target_index(index_of_language(&settings.target_language));
        win.set_mode_index(if settings.mode == "translation" { 1 } else { 0 });
        win.set_hotkey(settings.hot_key.as_str().into());
        win.set_ctrl_mod(settings.modifiers.contains(ModifierKeys::CONTROL));
        win.set_alt_mod(settings.modifiers.contains(ModifierKeys::ALT));
        win.set_shift_mod(settings.modifiers.contains(ModifierKeys::SHIFT));
        win.set_win_mod(settings.modifiers.contains(ModifierKeys::WIN));
        win.set_theme_index(if settings.theme == "light" { 1 } else { 0 });
        win.set_color_hex(normalize_hex(&settings.border_color).into());
        win.set_border_opacity(settings.border_opacity as f32);
        win.set_multicolor(settings.multicolor_border);
        win.set_api_key1(settings.api_key1.as_str().into());
        win.set_api_key2(settings.api_key2.as_str().into());
        win.set_use_gpu(settings.use_gpu_acceleration);
        win.set_view(view);
        win.set_dark(self.visual.borrow().dark);
        self.refresh_management();
    }

    fn refresh_management(&self) {
        let win = self
            .management
            .borrow()
            .as_ref()
            .map(|win| win.clone_strong());
        let Some(win) = win else { return };
        let dark = self.visual.borrow().dark;
        if win.get_dark() != dark {
            win.set_dark(dark);
        }

        let rows = self.model_rows();
        let hash = hash_model_rows(&rows);
        if hash != self.last_model_rows_hash.get() {
            self.last_model_rows_hash.set(hash);
            win.set_model_rows(ModelRc::new(VecModel::from(rows)));
        }

        let usage = self.usage_texts();
        if *self.last_usage_text.borrow() != usage {
            win.set_usage1(usage.0.as_str().into());
            win.set_usage2(usage.1.as_str().into());
            *self.last_usage_text.borrow_mut() = usage;
        }
    }

    fn model_rows(&self) -> Vec<ModelRow> {
        let settings = self.core.settings.get();
        let pending_delete = self.pending_delete.borrow().clone();
        whisper::list_models()
            .into_iter()
            .map(|m| {
                let busy = self.core.model_is_downloading(m.name);
                let progress = self.core.model_progress(m.name).unwrap_or(0) as i32;
                let confirming = pending_delete.as_ref().is_some_and(|(name, at)| {
                    name == m.name && at.elapsed().as_millis() < DELETE_CONFIRM_MS as u128
                });
                let size = match m.size_on_disk {
                    Some(bytes) => format!("{}  ·  {}", m.size_label, fmt_size(bytes)),
                    None => m.size_label.to_string(),
                };
                ModelRow {
                    name: m.name.into(),
                    title: m.label.into(),
                    size: size.into(),
                    downloaded: m.downloaded,
                    active: settings.local_model_name == m.name,
                    busy,
                    progress,
                    confirming,
                }
            })
            .collect()
    }

    fn usage_texts(&self) -> (String, String) {
        let settings = self.core.settings.get();
        let (u1, total1, u2, total2, has1, has2) = self.core.usage();
        (
            format!(
                "{}  ·  {}/{} este minuto",
                mask_key(&settings.api_key1, has1),
                u1,
                total1
            ),
            format!(
                "{}  ·  {}/{} este minuto",
                mask_key(&settings.api_key2, has2),
                u2,
                total2
            ),
        )
    }

    fn save_settings(self: &Rc<Self>) {
        let win = self
            .management
            .borrow()
            .as_ref()
            .map(|win| win.clone_strong());
        let Some(win) = win else { return };

        let language_idx = win
            .get_language_index()
            .clamp(0, ALL_LANGUAGES.len() as i32 - 1) as usize;
        let target_idx = win
            .get_target_index()
            .clamp(0, ALL_LANGUAGES.len() as i32 - 1) as usize;
        let language = ALL_LANGUAGES[language_idx].code.to_string();
        let target = ALL_LANGUAGES[target_idx].code.to_string();
        let mode = if win.get_mode_index() == 1 {
            "translation"
        } else {
            "transcription"
        }
        .to_string();
        let theme = if win.get_theme_index() == 1 {
            "light"
        } else {
            "dark"
        }
        .to_string();

        let key = win.get_hotkey().trim().to_uppercase();
        if !valid_hotkey(&key) {
            self.core
                .error("Atajo inválido. Usa una tecla como F8, A, D1 o Space.");
            self.pump_messages();
            self.update_toast();
            return;
        }
        let mut modifiers = 0u8;
        if win.get_ctrl_mod() {
            modifiers |= ModifierKeys::CONTROL.0;
        }
        if win.get_alt_mod() {
            modifiers |= ModifierKeys::ALT.0;
        }
        if win.get_shift_mod() {
            modifiers |= ModifierKeys::SHIFT.0;
        }
        if win.get_win_mod() {
            modifiers |= ModifierKeys::WIN.0;
        }
        let color = match parse_color(&win.get_color_hex()) {
            Some(rgb) => format!("#{:02X}{:02X}{:02X}", rgb.0, rgb.1, rgb.2),
            None => {
                self.core
                    .error("Color de borde inválido. Usa un valor hexadecimal como #8AB4FF.");
                self.pump_messages();
                self.update_toast();
                return;
            }
        };
        let opacity = win.get_border_opacity().round().clamp(0.0, 255.0) as u8;
        let multicolor = win.get_multicolor();

        self.core.settings.update(|s| {
            s.language = language.clone();
            s.app_language = language.clone();
            s.mode = mode.clone();
            s.target_language = target.clone();
            s.hot_key = key.clone();
            s.modifiers = ModifierKeys(modifiers);
            s.theme = theme.clone();
            s.border_color = color.clone();
            s.border_opacity = opacity;
            s.multicolor_border = multicolor;
        });

        if let Err(e) = self.register_current_hotkey() {
            self.core
                .error(format!("No se pudo registrar el atajo: {e}"));
        }
        self.last_model_rows_hash.set(u64::MAX);
        self.apply_visuals();
        self.sync_pill_state();
        self.refresh_management();
        self.close_management();
        self.pump_messages();
        self.update_toast();
    }

    fn save_keys(&self) {
        let win = self
            .management
            .borrow()
            .as_ref()
            .map(|win| win.clone_strong());
        let Some(win) = win else { return };
        self.core.set_api_keys(
            win.get_api_key1().to_string(),
            win.get_api_key2().to_string(),
        );
        self.last_usage_text.borrow_mut().0.clear();
        self.last_usage_text.borrow_mut().1.clear();
        self.refresh_management();
        self.close_management();
    }

    fn model_action(&self, action: &str, name: &str) {
        match action {
            "download" => {
                self.core.start_download(name.to_string());
                self.last_model_rows_hash.set(u64::MAX);
            }
            "cancel" => self.core.cancel_download(),
            "use" => {
                let gpu = self
                    .management
                    .borrow()
                    .as_ref()
                    .map(|win| win.get_use_gpu())
                    .unwrap_or_else(|| self.core.settings.get().use_gpu_acceleration);
                self.core.set_local_model(name.to_string(), gpu);
                self.last_model_rows_hash.set(u64::MAX);
            }
            "delete" => {
                let now = Instant::now();
                let confirmed =
                    self.pending_delete
                        .borrow()
                        .as_ref()
                        .is_some_and(|(pending, at)| {
                            pending == name && at.elapsed().as_millis() < DELETE_CONFIRM_MS as u128
                        });
                if confirmed {
                    *self.pending_delete.borrow_mut() = None;
                    self.core.delete_model(name);
                    self.last_model_rows_hash.set(u64::MAX);
                } else {
                    *self.pending_delete.borrow_mut() = Some((name.to_string(), now));
                    self.last_model_rows_hash.set(u64::MAX);
                }
            }
            _ => {}
        }
        self.refresh_management();
    }

    fn set_gpu(&self, enabled: bool) {
        let model = self.core.settings.get().local_model_name;
        self.core.set_local_model(model, enabled);
        self.refresh_management();
    }

    // ——— tray / quit ———

    fn quit(&self) {
        if self.quitting.replace(true) {
            return;
        }
        self.save_position_now();
        if let Some(tray) = &self.tray {
            let _ = tray.hide();
        }
        if let Some(menu) = self.menu.borrow_mut().take() {
            let _ = menu.hide();
        }
        if let Some(win) = self.management.borrow_mut().take() {
            let _ = win.hide();
        }
        if let Some(toast) = self.toast.borrow_mut().take() {
            let _ = toast.hide();
        }
        let _ = self.pill.hide();
        slint::quit_event_loop().ok();
        // Don't let the Tokio runtime's blocking pool delay a user-requested
        // exit (a whisper transcription can take a while to unwind).
        std::process::exit(0);
    }
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

fn base_accent(visual: &PillVisual) -> Color {
    Color::from_argb_u8(
        (visual.opacity * 255.0).round().clamp(0.0, 255.0) as u8,
        visual.rgb.0,
        visual.rgb.1,
        visual.rgb.2,
    )
}

fn color_scheme(dark: bool) -> slint::private_unstable_api::re_exports::ColorScheme {
    use slint::private_unstable_api::re_exports::ColorScheme;
    if dark {
        ColorScheme::Dark
    } else {
        ColorScheme::Light
    }
}

fn apply_management_palette(component: &ManagementWindow, dark: bool) {
    Palette::get(component).set_color_scheme(color_scheme(dark));
}

fn index_of_language(code: &str) -> i32 {
    ALL_LANGUAGES
        .iter()
        .position(|lg| lg.code.eq_ignore_ascii_case(code))
        .unwrap_or(0) as i32
}

fn valid_hotkey(key: &str) -> bool {
    if key.is_empty() {
        return false;
    }
    let upper = key.to_ascii_uppercase();
    if matches!(
        upper.as_str(),
        "NONE"
            | "ESCAPE"
            | "CONTROL"
            | "CONTROLKEY"
            | "LCONTROLKEY"
            | "RCONTROLKEY"
            | "MENU"
            | "LMENU"
            | "RMENU"
            | "SHIFT"
            | "SHIFTKEY"
            | "LSHIFTKEY"
            | "RSHIFTKEY"
            | "LWIN"
            | "RWIN"
            | "WIN"
    ) {
        return false;
    }
    hotkey_names::code_name(key).is_some()
}

fn mask_key(key: &str, has_key: bool) -> String {
    if !has_key || key.is_empty() {
        return "Sin configurar".into();
    }
    let suffix: String = key
        .chars()
        .rev()
        .take(4)
        .collect::<String>()
        .chars()
        .rev()
        .collect();
    format!("****{suffix}")
}

fn fmt_size(bytes: u64) -> String {
    let mb = bytes as f64 / (1024.0 * 1024.0);
    if mb > 1024.0 {
        format!("{:.2} GB", mb / 1024.0)
    } else {
        format!("{:.0} MB", mb)
    }
}

fn hash_model_rows(rows: &[ModelRow]) -> u64 {
    let mut hasher = DefaultHasher::new();
    for row in rows {
        row.name.hash(&mut hasher);
        row.downloaded.hash(&mut hasher);
        row.active.hash(&mut hasher);
        row.busy.hash(&mut hasher);
        row.progress.hash(&mut hasher);
        row.confirming.hash(&mut hasher);
        row.size.hash(&mut hasher);
    }
    hasher.finish()
}

fn parse_color(value: &str) -> Option<(u8, u8, u8)> {
    let value = value.trim();
    if let Some(hex) = value.strip_prefix('#') {
        let bytes = |s: &str| u8::from_str_radix(s, 16).ok();
        return match hex.len() {
            3 => Some((
                bytes(&hex[0..1].repeat(2))?,
                bytes(&hex[1..2].repeat(2))?,
                bytes(&hex[2..3].repeat(2))?,
            )),
            6 => Some((bytes(&hex[0..2])?, bytes(&hex[2..4])?, bytes(&hex[4..6])?)),
            8 => Some((bytes(&hex[0..2])?, bytes(&hex[2..4])?, bytes(&hex[4..6])?)),
            _ => None,
        };
    }
    // Legacy C# Color.Name values ("Blue", "Lime", ...).
    let name = value.to_ascii_lowercase();
    Some(match name.as_str() {
        "black" => (0, 0, 0),
        "white" => (255, 255, 255),
        "red" => (255, 0, 0),
        "lime" | "green" => (0, 255, 0),
        "blue" => (0, 0, 255),
        "yellow" => (255, 255, 0),
        "cyan" | "aqua" => (0, 255, 255),
        "magenta" | "fuchsia" => (255, 0, 255),
        "gray" | "grey" => (128, 128, 128),
        "silver" => (192, 192, 192),
        "maroon" => (128, 0, 0),
        "olive" => (128, 128, 0),
        "navy" => (0, 0, 128),
        "teal" => (0, 128, 128),
        "purple" => (128, 0, 128),
        "orange" => (255, 165, 0),
        "pink" => (255, 192, 203),
        "brown" => (165, 42, 42),
        "gold" => (255, 215, 0),
        "indigo" => (75, 0, 130),
        "violet" => (238, 130, 238),
        "turquoise" => (64, 224, 208),
        "salmon" => (250, 128, 114),
        "crimson" => (220, 20, 60),
        "darkblue" => (0, 0, 139),
        "darkgreen" => (0, 100, 0),
        "darkred" => (139, 0, 0),
        "lightblue" => (173, 216, 230),
        "lightgreen" => (144, 238, 144),
        "transparent" => (255, 255, 255),
        _ => return None,
    })
}

fn normalize_hex(value: &str) -> String {
    parse_color(value)
        .map(|(r, g, b)| format!("#{r:02X}{g:02X}{b:02X}"))
        .unwrap_or_else(|| "#FFFFFF".into())
}

fn hsv_to_rgb(h: f32, s: f32, v: f32) -> (u8, u8, u8) {
    let h = h.rem_euclid(360.0) / 60.0;
    let c = v * s;
    let x = c * (1.0 - (h % 2.0 - 1.0).abs());
    let (r1, g1, b1) = match h as u32 {
        0 => (c, x, 0.0),
        1 => (x, c, 0.0),
        2 => (0.0, c, x),
        3 => (0.0, x, c),
        4 => (x, 0.0, c),
        _ => (c, 0.0, x),
    };
    let m = v - c;
    (
        ((r1 + m) * 255.0).round().clamp(0.0, 255.0) as u8,
        ((g1 + m) * 255.0).round().clamp(0.0, 255.0) as u8,
        ((b1 + m) * 255.0).round().clamp(0.0, 255.0) as u8,
    )
}

// ---------------------------------------------------------------------------
// Native window helpers (Windows)
// ---------------------------------------------------------------------------

#[cfg(windows)]
fn hwnd_of(window: &slint::Window) -> Option<isize> {
    use raw_window_handle::{HasWindowHandle, RawWindowHandle};
    match window.window_handle().window_handle().ok()?.as_raw() {
        RawWindowHandle::Win32(handle) => Some(handle.hwnd.get()),
        _ => None,
    }
}

#[cfg(not(windows))]
fn hwnd_of(_window: &slint::Window) -> Option<isize> {
    None
}

#[cfg(windows)]
fn apply_tool_window(
    window: &slint::Window,
    no_activate: bool,
    topmost: bool,
    rounded: bool,
) -> bool {
    use windows_sys::Win32::UI::WindowsAndMessaging::{
        GetWindowLongPtrW, IsWindowVisible, SetWindowLongPtrW, SetWindowPos, ShowWindow,
        GWL_EXSTYLE, GWL_STYLE, HWND_TOP, HWND_TOPMOST, SWP_FRAMECHANGED, SWP_NOACTIVATE,
        SWP_NOMOVE, SWP_NOSIZE, SW_HIDE, SW_SHOW, SW_SHOWNOACTIVATE, WS_BORDER, WS_CAPTION,
        WS_DLGFRAME, WS_EX_APPWINDOW, WS_EX_NOACTIVATE, WS_EX_TOOLWINDOW, WS_MAXIMIZEBOX,
        WS_MINIMIZEBOX, WS_POPUP, WS_SYSMENU, WS_THICKFRAME,
    };
    let Some(hwnd) = hwnd_of(window) else {
        return false;
    };
    let hwnd = hwnd as _;
    unsafe {
        let before = GetWindowLongPtrW(hwnd, GWL_EXSTYLE);
        let mut style = before;
        style |= WS_EX_TOOLWINDOW as isize;
        style &= !(WS_EX_APPWINDOW as isize);
        if no_activate {
            style |= WS_EX_NOACTIVATE as isize;
        } else {
            style &= !(WS_EX_NOACTIVATE as isize);
        }
        SetWindowLongPtrW(hwnd, GWL_EXSTYLE, style);
        let after = GetWindowLongPtrW(hwnd, GWL_EXSTYLE);
        log::info!(
            "tool window chrome: hwnd=0x{:X} ex_before=0x{before:X} ex_after=0x{after:X}",
            hwnd as isize
        );
        if rounded {
            // winit keeps WS_CAPTION/WS_SYSMENU internally even for
            // `no-frame: true`. While dragging, Windows can then paint the
            // native title bar (light blue "VoiceToText" strip) over the pill.
            // Turn it into a true popup; the fixed 121x32 region below keeps
            // the visible widget size and rounded silhouette.
            let mut base_style = GetWindowLongPtrW(hwnd, GWL_STYLE);
            base_style &= !((WS_CAPTION
                | WS_BORDER
                | WS_DLGFRAME
                | WS_THICKFRAME
                | WS_SYSMENU
                | WS_MINIMIZEBOX
                | WS_MAXIMIZEBOX) as isize);
            base_style |= WS_POPUP as isize;
            SetWindowLongPtrW(hwnd, GWL_STYLE, base_style);
        }
        SetWindowPos(
            hwnd,
            if topmost { HWND_TOPMOST } else { HWND_TOP },
            0,
            0,
            0,
            0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_FRAMECHANGED,
        );

        // Changing GWL_EXSTYLE on an already-visible window does not always
        // remove its taskbar button. Hide/show once, and apply the pill region
        // while hidden so the square window is never visible.
        let visible = IsWindowVisible(hwnd) != 0;
        if visible {
            ShowWindow(hwnd, SW_HIDE);
        }
        if rounded {
            apply_rounded_region(window);
        }
        if visible {
            ShowWindow(
                hwnd,
                if no_activate {
                    SW_SHOWNOACTIVATE
                } else {
                    SW_SHOW
                },
            );
        }
    }
    true
}

#[cfg(not(windows))]
fn apply_tool_window(
    _window: &slint::Window,
    _no_activate: bool,
    _topmost: bool,
    _rounded: bool,
) -> bool {
    true
}

#[cfg(windows)]
fn apply_rounded_region(window: &slint::Window) {
    use windows_sys::Win32::Graphics::Gdi::{CreateRoundRectRgn, DeleteObject, SetWindowRgn};

    let Some(hwnd) = hwnd_of(window) else { return };
    let hwnd = hwnd as _;
    // winit can grow the native window to 136x39 to accommodate its internal
    // frame bookkeeping. The visible pill is always 121x32 logical pixels, so
    // shape the region from those fixed dimensions and the current DPI scale.
    let scale = window.scale_factor().max(0.5);
    let width = (PILL_WIDTH * scale).round() as i32;
    let height = (PILL_HEIGHT * scale).round() as i32;
    if width <= 0 || height <= 0 {
        return;
    }
    unsafe {
        // Ellipse diameter = pill height: the ends are perfect semicircles.
        let region = CreateRoundRectRgn(0, 0, width + 1, height + 1, height, height);
        if region.is_null() {
            log::warn!("CreateRoundRectRgn failed");
            return;
        }
        if SetWindowRgn(hwnd, region, 1) == 0 {
            let _ = DeleteObject(region as _);
            log::warn!("SetWindowRgn failed");
        } else {
            log::info!("pill region applied: {width}x{height} (scale {scale})");
        }
    }
}

#[cfg(not(windows))]
fn apply_rounded_region(_window: &slint::Window) {}

#[cfg(windows)]
fn focus_window(window: &slint::Window) {
    use windows_sys::Win32::UI::WindowsAndMessaging::{
        SetForegroundWindow, SetWindowPos, HWND_TOP, SWP_NOACTIVATE, SWP_NOMOVE, SWP_NOSIZE,
    };
    let Some(hwnd) = hwnd_of(window) else { return };
    let hwnd = hwnd as _;
    unsafe {
        SetWindowPos(
            hwnd,
            HWND_TOP,
            0,
            0,
            0,
            0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE,
        );
        SetForegroundWindow(hwnd);
    }
}

#[cfg(not(windows))]
fn focus_window(_window: &slint::Window) {}

#[cfg(windows)]
fn center_window(window: &slint::Window) {
    use windows_sys::Win32::Foundation::RECT;
    use windows_sys::Win32::Graphics::Gdi::{
        GetMonitorInfoW, MonitorFromWindow, MONITORINFO, MONITOR_DEFAULTTONEAREST,
    };
    use windows_sys::Win32::UI::WindowsAndMessaging::{
        GetWindowRect, SetWindowPos, HWND_TOP, SWP_NOACTIVATE, SWP_NOSIZE,
    };
    let Some(hwnd) = hwnd_of(window) else { return };
    let hwnd = hwnd as _;
    unsafe {
        let mut rect: RECT = std::mem::zeroed();
        if GetWindowRect(hwnd, &mut rect) == 0 {
            return;
        }
        let monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
        if monitor.is_null() {
            return;
        }
        let mut info: MONITORINFO = std::mem::zeroed();
        info.cbSize = std::mem::size_of::<MONITORINFO>() as u32;
        if GetMonitorInfoW(monitor, &mut info) == 0 {
            return;
        }
        let width = rect.right - rect.left;
        let height = rect.bottom - rect.top;
        let work = info.rcWork;
        let x = work.left + ((work.right - work.left) - width) / 2;
        let y = work.top + ((work.bottom - work.top) - height) / 2;
        SetWindowPos(hwnd, HWND_TOP, x, y, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE);
    }
}

#[cfg(not(windows))]
fn center_window(_window: &slint::Window) {}

/// Bounds of the visible 121x32 pill, ignoring the extra native client area
/// winit reserves around it. Used to anchor the menu and toast.
#[cfg(windows)]
fn visible_pill_rect(pill: &slint::Window) -> Option<windows_sys::Win32::Foundation::RECT> {
    use windows_sys::Win32::Foundation::RECT;
    use windows_sys::Win32::UI::WindowsAndMessaging::GetWindowRect;

    let hwnd = hwnd_of(pill)? as _;
    unsafe {
        let mut outer: RECT = std::mem::zeroed();
        if GetWindowRect(hwnd, &mut outer) == 0 {
            return None;
        }
        let scale = pill.scale_factor().max(0.5);
        let width = (PILL_WIDTH * scale).round() as i32;
        let height = (PILL_HEIGHT * scale).round() as i32;
        Some(RECT {
            left: outer.left,
            top: outer.top,
            right: outer.left + width,
            bottom: outer.top + height,
        })
    }
}

#[cfg(windows)]
fn position_toast(toast: &slint::Window, pill: &slint::Window) {
    use windows_sys::Win32::Foundation::RECT;
    use windows_sys::Win32::Graphics::Gdi::{
        GetMonitorInfoW, MonitorFromWindow, MONITORINFO, MONITOR_DEFAULTTONEAREST,
    };
    use windows_sys::Win32::UI::WindowsAndMessaging::{
        GetWindowRect, SetWindowPos, HWND_TOP, SWP_NOACTIVATE, SWP_NOSIZE,
    };
    let (Some(pill_hwnd), Some(toast_hwnd)) = (hwnd_of(pill), hwnd_of(toast)) else {
        return;
    };
    let Some(pill_rect) = visible_pill_rect(pill) else {
        return;
    };
    let (pill_hwnd, toast_hwnd) = (pill_hwnd as _, toast_hwnd as _);
    unsafe {
        let mut toast_rect: RECT = std::mem::zeroed();
        if GetWindowRect(toast_hwnd, &mut toast_rect) == 0 {
            return;
        }
        let monitor = MonitorFromWindow(pill_hwnd, MONITOR_DEFAULTTONEAREST);
        let mut work = RECT {
            left: 0,
            top: 0,
            right: 1920,
            bottom: 1080,
        };
        if !monitor.is_null() {
            let mut info: MONITORINFO = std::mem::zeroed();
            info.cbSize = std::mem::size_of::<MONITORINFO>() as u32;
            if GetMonitorInfoW(monitor, &mut info) != 0 {
                work = info.rcWork;
            }
        }
        let width = toast_rect.right - toast_rect.left;
        let height = toast_rect.bottom - toast_rect.top;
        let gap = 8;
        let mut x = pill_rect.left + ((pill_rect.right - pill_rect.left) - width) / 2;
        x = x.clamp(
            work.left + gap,
            (work.right - width - gap).max(work.left + gap),
        );
        let mut y = pill_rect.bottom + gap;
        if y + height > work.bottom - gap {
            y = pill_rect.top - height - gap;
        }
        y = y.clamp(
            work.top + gap,
            (work.bottom - height - gap).max(work.top + gap),
        );
        SetWindowPos(
            toast_hwnd,
            HWND_TOP,
            x,
            y,
            0,
            0,
            SWP_NOSIZE | SWP_NOACTIVATE,
        );
    }
}

#[cfg(not(windows))]
fn position_toast(_toast: &slint::Window, _pill: &slint::Window) {}

#[cfg(windows)]
fn foreground_window() -> Option<isize> {
    use windows_sys::Win32::UI::WindowsAndMessaging::GetForegroundWindow;
    let hwnd = unsafe { GetForegroundWindow() };
    (!hwnd.is_null()).then_some(hwnd as isize)
}

#[cfg(not(windows))]
fn foreground_window() -> Option<isize> {
    None
}

#[cfg(windows)]
fn position_menu(menu: &slint::Window, pill: &slint::Window) {
    use windows_sys::Win32::Foundation::RECT;
    use windows_sys::Win32::Graphics::Gdi::{
        GetMonitorInfoW, MonitorFromWindow, MONITORINFO, MONITOR_DEFAULTTONEAREST,
    };
    use windows_sys::Win32::UI::WindowsAndMessaging::{
        GetWindowRect, SetWindowPos, HWND_TOP, SWP_NOACTIVATE, SWP_NOSIZE,
    };
    let (Some(menu_hwnd), Some(pill_hwnd)) = (hwnd_of(menu), hwnd_of(pill)) else {
        return;
    };
    let Some(pill_rect) = visible_pill_rect(pill) else {
        return;
    };
    let (menu_hwnd, pill_hwnd) = (menu_hwnd as _, pill_hwnd as _);
    unsafe {
        let mut menu_rect: RECT = std::mem::zeroed();
        if GetWindowRect(menu_hwnd, &mut menu_rect) == 0 {
            return;
        }
        let monitor = MonitorFromWindow(pill_hwnd, MONITOR_DEFAULTTONEAREST);
        let mut work = RECT {
            left: 0,
            top: 0,
            right: 1920,
            bottom: 1080,
        };
        if !monitor.is_null() {
            let mut info: MONITORINFO = std::mem::zeroed();
            info.cbSize = std::mem::size_of::<MONITORINFO>() as u32;
            if GetMonitorInfoW(monitor, &mut info) != 0 {
                work = info.rcWork;
            }
        }
        let width = menu_rect.right - menu_rect.left;
        let height = menu_rect.bottom - menu_rect.top;
        let gap = 6;
        let x = (pill_rect.right - width).clamp(
            work.left + gap,
            (work.right - width - gap).max(work.left + gap),
        );
        let mut y = pill_rect.bottom + gap;
        if y + height > work.bottom - gap {
            y = pill_rect.top - height - gap;
        }
        y = y.clamp(
            work.top + gap,
            (work.bottom - height - gap).max(work.top + gap),
        );
        SetWindowPos(menu_hwnd, HWND_TOP, x, y, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE);
    }
}

#[cfg(not(windows))]
fn position_menu(_menu: &slint::Window, _pill: &slint::Window) {}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn parses_hex_and_legacy_named_colors() {
        assert_eq!(parse_color("#0000FF"), Some((0, 0, 255)));
        assert_eq!(parse_color("#abc"), Some((170, 187, 204)));
        assert_eq!(parse_color("Blue"), Some((0, 0, 255)));
        assert_eq!(parse_color("not-a-color"), None);
    }

    #[test]
    fn normalizes_colors_for_the_settings_field() {
        assert_eq!(normalize_hex("Blue"), "#0000FF");
        assert_eq!(normalize_hex("#8ab4ff"), "#8AB4FF");
    }

    #[test]
    fn hotkey_validation_rejects_modifiers_only() {
        assert!(valid_hotkey("F8"));
        assert!(valid_hotkey("A"));
        assert!(!valid_hotkey("Ctrl"));
        assert!(!valid_hotkey("None"));
    }
}
