// Popup menu: opens from the pill click. Sections expand inline (accordion).
// Data arrives via the "menu-data" event; the window closes on blur (Rust).
import { listen, invoke, getLanguages, type AppSettings, type Usage } from "./shared/api";
import { makeT } from "./shared/i18n";

interface MenuData {
  settings: AppSettings;
  usage: Usage;
}

let data: MenuData | null = null;
let lang = "es";
let t = makeT(lang);
let langNames = new Map<string, string>();
getLanguages()
  .then(({ all }) => {
    langNames = new Map(all.map((l) => [l.code, l.name]));
  })
  .catch(() => {});

const menu = document.getElementById("menu") as HTMLDivElement;

// Inline SVG icons (stroke-based, 15px)
const icons: Record<string, string> = {
  settings: `<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 1 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 1 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 1 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 1 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"/></svg>`,
  key: `<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M21 2l-2 2m-7.61 7.61a5.5 5.5 0 1 1-7.778 7.778 5.5 5.5 0 0 1 7.777-7.777zm0 0L15.5 7.5m0 0l3 3L22 7l-3-3m-3.5 3.5L19 4"/></svg>`,
  provider: `<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><rect x="2" y="2" width="20" height="8" rx="2"/><rect x="2" y="14" width="20" height="8" rx="2"/><line x1="6" y1="6" x2="6.01" y2="6"/><line x1="6" y1="18" x2="6.01" y2="18"/></svg>`,
  mode: `<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M12 2a3 3 0 0 0-3 3v7a3 3 0 0 0 6 0V5a3 3 0 0 0-3-3z"/><path d="M19 10v2a7 7 0 0 1-14 0v-2"/><line x1="12" y1="19" x2="12" y2="22"/></svg>`,
  lang: `<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="2" y1="12" x2="22" y2="12"/><path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z"/></svg>`,
  hide: `<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"/><line x1="1" y1="1" x2="23" y2="23"/></svg>`,
  exit: `<svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" y1="12" x2="9" y2="12"/></svg>`,
  chevron: `<svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><polyline points="9 18 15 12 9 6"/></svg>`,
  check: `<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round"><polyline points="20 6 9 17 4 12"/></svg>`,
};

function render() {
  if (!data) return;
  lang = data.settings.AppLanguage || "es";
  t = makeT(lang);
  const s = data.settings;

  menu.innerHTML = "";

  // —— settings ——
  menu.appendChild(item(icons.settings, t("menu_config"), () => {
    invoke("close_menu");
    invoke("show_window", { label: "settings" });
  }));

  // —— api keys ——
  menu.appendChild(item(icons.key, t("menu_apikeys"), () => {
    invoke("close_menu");
    invoke("show_window", { label: "keys" });
  }));

  menu.appendChild(sep());

  // —— provider (accordion) ——
  const providerLabel =
    s.ActiveProvider === "LocalWhisper" ? t("menu_local") : t("menu_groq");
  const providerSub = sub();
  providerSub.appendChild(subItem(t("menu_groq"), s.ActiveProvider === "GroqApi", () =>
    invoke("set_provider", { provider: "GroqApi" })));
  providerSub.appendChild(subItem(t("menu_local"), s.ActiveProvider === "LocalWhisper", () =>
    invoke("set_provider", { provider: "LocalWhisper" })));
  providerSub.appendChild(subItem(t("menu_model"), false, () => {
    invoke("close_menu");
    invoke("show_window", { label: "models" });
  }));
  menu.appendChild(
    accordionItem(icons.provider, t("menu_provider"), providerLabel, providerSub)
  );

  // —— mode (accordion) ——
  const modeLabel = s.Mode === "translation" ? t("mode_translation") : t("mode_transcription");
  const modeSub = sub();
  modeSub.appendChild(subItem(t("mode_transcription"), s.Mode === "transcription", () =>
    invoke("set_mode", { mode: "transcription" })));
  modeSub.appendChild(subItem(t("mode_translation"), s.Mode === "translation", () =>
    invoke("set_mode", { mode: "translation" })));
  menu.appendChild(accordionItem(icons.mode, t("menu_mode"), modeLabel, modeSub));

  // —— target language (only in translation mode) ——
  if (s.Mode === "translation") {
    const targetSub = sub();
    targetSub.style.maxHeight = "160px";
    targetSub.style.overflowY = "auto";
    for (const code of ["es", "en", "pt", "fr", "de", "it", "ja", "ko", "zh", "ru"]) {
      targetSub.appendChild(
        subItem(`${targetName(code)} (${code})`, s.TargetLanguage === code, () =>
          invoke("set_target_language", { code }))
      );
    }
    menu.appendChild(
      accordionItem(icons.lang, t("menu_target_lang"), targetName(s.TargetLanguage), targetSub)
    );
  }

  menu.appendChild(sep());

  // —— hide ——
  const hide = item(icons.hide, t("menu_hide"), () => {
    invoke("close_menu");
    invoke("hide_pill");
  });
  if (state !== "idle") (hide as HTMLButtonElement).disabled = true;
  menu.appendChild(hide);

  // —— exit ——
  const exit = item(icons.exit, t("menu_exit"), () => invoke("quit_app"));
  if (state !== "idle") (exit as HTMLButtonElement).disabled = true;
  menu.appendChild(exit);

  requestAnimationFrame(syncHeight);
}

let state = "idle";

function targetName(code: string): string {
  return langNames.get(code) ?? code.toUpperCase();
}

function item(
  icon: string,
  label: string,
  onClick: () => void
): HTMLButtonElement {
  const btn = document.createElement("button");
  btn.className = "menu-item";
  btn.innerHTML = `<span class="mi-icon">${icon}</span><span class="mi-label">${label}</span>`;
  btn.addEventListener("click", onClick);
  return btn;
}

function accordionItem(
  icon: string,
  label: string,
  value: string,
  subEl: HTMLDivElement
): HTMLDivElement {
  const wrap = document.createElement("div");
  wrap.className = "menu-section";
  const btn = document.createElement("button");
  btn.className = "menu-item";
  btn.innerHTML = `<span class="mi-icon">${icon}</span><span class="mi-label">${label}</span><span class="mi-value">${value}</span><span class="mi-icon chev">${icons.chevron}</span>`;
  const chev = btn.querySelector(".chev") as HTMLElement;
  btn.addEventListener("click", () => {
    subEl.classList.toggle("open");
    chev.style.transform = subEl.classList.contains("open") ? "rotate(90deg)" : "";
    requestAnimationFrame(syncHeight);
  });
  wrap.appendChild(btn);
  wrap.appendChild(subEl);
  return wrap;
}

function sub(): HTMLDivElement {
  const el = document.createElement("div");
  el.className = "menu-sub";
  return el;
}

function subItem(label: string, checked: boolean, onClick: () => void): HTMLButtonElement {
  const btn = document.createElement("button");
  btn.className = "menu-item" + (checked ? " checked" : "");
  btn.innerHTML = `<span class="mi-check">${icons.check}</span><span class="mi-label">${label}</span>`;
  btn.addEventListener("click", onClick);
  return btn;
}

function sep(): HTMLDivElement {
  const el = document.createElement("div");
  el.className = "menu-sep";
  return el;
}

function syncHeight() {
  const h = menu.scrollHeight + 10;
  invoke("resize_menu", { height: Math.min(h, 620) }).catch(() => {});
}

listen("menu-data", (payload) => {
  data = payload as MenuData;
  render();
});

listen("settings-changed", (payload) => {
  const p = payload as { settings: AppSettings };
  if (data) {
    data.settings = p.settings;
    render();
  }
});

listen("state-changed", (payload) => {
  const p = payload as { state: string };
  state = p.state;
  if (data) render();
});

listen("menu-refresh", () => {
  // re-anchor: Rust recomputes position on open; here just re-sync height
  requestAnimationFrame(syncHeight);
});
