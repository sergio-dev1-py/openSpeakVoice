// Settings window: language, mode, hotkey capture, appearance.
import { getSettings, getLanguages, setState, hideWindow, type LangEntry } from "./shared/api";
import { makeT } from "./shared/i18n";

let t = makeT("es");
let common: LangEntry[] = [];
let all: LangEntry[] = [];

// working copy (committed on Save)
const work = {
  language: "es",
  mode: "transcription",
  target: "en",
  hotkey: "F8",
  modifiers: "None",
  color: "#FFFFFF",
  opacity: 255,
  multicolor: false,
};

const $ = <T extends HTMLElement>(id: string) => document.getElementById(id) as T;

// —— init ——
const [s, langs] = await Promise.all([getSettings(), getLanguages()]);
common = langs.common;
all = langs.all;
Object.assign(work, {
  language: s.AppLanguage,
  mode: s.Mode,
  target: s.TargetLanguage,
  hotkey: s.HotKey,
  modifiers: s.Modifiers,
  color: s.BorderColor,
  opacity: s.BorderOpacity,
  multicolor: s.MulticolorBorder,
});
t = makeT(s.AppLanguage);
applyI18n();
renderLangChips();
renderModeChips();
renderTargetChips();
renderModifierChips();
initHotkeyCapture();
initAppearance();
initButtons();

function applyI18n() {
  $("title").textContent = t("settings_title");
  for (const el of document.querySelectorAll("[data-i18n]")) {
    el.textContent = t(el.getAttribute("data-i18n")!);
  }
  for (const el of document.querySelectorAll("[data-i18n-ph]")) {
    (el as HTMLInputElement).placeholder = t(el.getAttribute("data-i18n-ph")!);
  }
}

// —— language ——
function langChip(entry: LangEntry, selected: boolean, onClick: () => void) {
  const b = document.createElement("button");
  b.className = "chip" + (selected ? " on" : "");
  b.textContent = `${entry.name} (${entry.code})`;
  b.addEventListener("click", onClick);
  return b;
}

function renderLangChips() {
  const box = $("lang-chips");
  box.innerHTML = "";
  for (const lg of common.slice(0, 8)) {
    box.appendChild(
      langChip(lg, work.language === lg.code, () => {
        work.language = lg.code;
        renderLangChips();
      })
    );
  }
}

$("more-langs").addEventListener("click", () => {
  const picker = $("lang-picker");
  picker.style.display = picker.style.display === "none" ? "flex" : "none";
  if (picker.style.display === "flex") {
    ($("lang-search") as HTMLInputElement).focus();
    renderLangList("");
  }
});

$("lang-search").addEventListener("input", (e) => {
  renderLangList((e.target as HTMLInputElement).value.toLowerCase());
});

function renderLangList(query: string) {
  const list = $("lang-list");
  list.innerHTML = "";
  for (const lg of all) {
    if (
      query &&
      !lg.name.toLowerCase().includes(query) &&
      !lg.code.toLowerCase().includes(query)
    )
      continue;
    const opt = document.createElement("div");
    opt.className = "lang-option" + (work.language === lg.code ? " selected" : "");
    opt.innerHTML = `<span>${lg.name}</span><span class="code">${lg.code}</span>`;
    opt.addEventListener("click", () => {
      work.language = lg.code;
      $("lang-picker").style.display = "none";
      renderLangChips();
    });
    list.appendChild(opt);
  }
}

// —— mode ——
function renderModeChips() {
  const box = $("mode-chips");
  box.innerHTML = "";
  for (const mode of ["transcription", "translation"] as const) {
    const b = document.createElement("button");
    b.className = "chip" + (work.mode === mode ? " on" : "");
    b.textContent = t(mode === "transcription" ? "mode_transcription" : "mode_translation");
    b.addEventListener("click", () => {
      work.mode = mode;
      renderModeChips();
      $("target-lang-box").style.display = mode === "translation" ? "flex" : "none";
    });
    box.appendChild(b);
  }
  $("target-lang-box").style.display = work.mode === "translation" ? "flex" : "none";
}

function renderTargetChips() {
  const box = $("target-chips");
  box.innerHTML = "";
  for (const lg of common.slice(0, 8)) {
    box.appendChild(
      langChip(lg, work.target === lg.code, () => {
        work.target = lg.code;
        renderTargetChips();
      })
    );
  }
}

$("more-target-langs").addEventListener("click", () => {
  const picker = $("target-picker");
  picker.style.display = picker.style.display === "none" ? "flex" : "none";
  if (picker.style.display === "flex") {
    ($("target-search") as HTMLInputElement).focus();
    renderTargetList("");
  }
});

$("target-search").addEventListener("input", (e) => {
  renderTargetList((e.target as HTMLInputElement).value.toLowerCase());
});

function renderTargetList(query: string) {
  const list = $("target-list");
  list.innerHTML = "";
  for (const lg of all) {
    if (
      query &&
      !lg.name.toLowerCase().includes(query) &&
      !lg.code.toLowerCase().includes(query)
    )
      continue;
    const opt = document.createElement("div");
    opt.className = "lang-option" + (work.target === lg.code ? " selected" : "");
    opt.innerHTML = `<span>${lg.name}</span><span class="code">${lg.code}</span>`;
    opt.addEventListener("click", () => {
      work.target = lg.code;
      $("target-picker").style.display = "none";
      renderTargetChips();
    });
    list.appendChild(opt);
  }
}

// —— hotkey capture ——
function modifierChips(): { key: string; label: string; flag: string }[] {
  return [
    { key: "Control", label: "Ctrl", flag: "Control" },
    { key: "Alt", label: "Alt", flag: "Alt" },
    { key: "Shift", label: "Shift", flag: "Shift" },
    { key: "Win", label: "Win", flag: "Win" },
  ];
}

function parseModifiers(s: string): Set<string> {
  const out = new Set<string>();
  for (const part of s.split(",")) {
    const p = part.trim();
    if (["Control", "Alt", "Shift", "Win"].includes(p)) out.add(p);
  }
  return out;
}

function renderModifierChips() {
  const box = $("modifier-chips");
  box.innerHTML = "";
  const active = parseModifiers(work.modifiers);
  for (const m of modifierChips()) {
    const chip = document.createElement("label");
    chip.className = "chip" + (active.has(m.flag) ? " on" : "");
    chip.innerHTML = `<input type="checkbox" ${active.has(m.flag) ? "checked" : ""}/>${m.label}`;
    chip.addEventListener("click", (e) => {
      e.preventDefault();
      if (active.has(m.flag)) active.delete(m.flag);
      else active.add(m.flag);
      work.modifiers = active.size === 0 ? "None" : [...active].join(", ");
      renderModifierChips();
    });
    box.appendChild(chip);
  }
}

const MODIFIER_ONLY = new Set([
  "ControlKey", "LControlKey", "RControlKey", "Menu", "LMenu", "RMenu",
  "ShiftKey", "LShiftKey", "RShiftKey", "LWin", "RWin", "None", "Escape",
]);

function keyName(e: KeyboardEvent): string | null {
  const code = e.code;
  if (/^Key[A-Z]$/.test(code)) return code.slice(3);
  if (/^Digit[0-9]$/.test(code)) return "D" + code.slice(5);
  if (/^Numpad[0-9]$/.test(code)) return "NumPad" + code.slice(6);
  if (/^F([1-9]|1[0-9]|2[0-4])$/.test(code)) return code;
  const map: Record<string, string> = {
    Space: "Space", Enter: "Enter", Escape: "Escape", Tab: "Tab",
    Backspace: "Back", Delete: "Delete", Insert: "Insert", Home: "Home",
    End: "End", PageUp: "PageUp", PageDown: "PageDown",
    ArrowUp: "Up", ArrowDown: "Down", ArrowLeft: "Left", ArrowRight: "Right",
    Backquote: "Oemtilde", Minus: "OemMinus", Equal: "Oemplus",
    BracketLeft: "OemOpenBrackets", BracketRight: "OemCloseBrackets",
    Backslash: "OemPipe", Semicolon: "OemSemicolon", Quote: "OemQuotes",
    Comma: "Oemcomma", Period: "OemPeriod", Slash: "OemQuestion",
    NumpadMultiply: "Multiply", NumpadAdd: "Add", NumpadSubtract: "Subtract",
    NumpadDivide: "Divide", NumpadDecimal: "Decimal",
  };
  return map[code] ?? null;
}

function initHotkeyCapture() {
  const box = $("hotkey-box");
  const refresh = () => {
    const mods: string[] = [];
    const active = parseModifiers(work.modifiers);
    if (active.has("Control")) mods.push("Ctrl");
    if (active.has("Alt")) mods.push("Alt");
    if (active.has("Shift")) mods.push("Shift");
    if (active.has("Win")) mods.push("Win");
    mods.push(work.hotkey);
    box.textContent = mods.join(" + ");
  };
  refresh();

  $("capture-btn").addEventListener("click", () => {
    box.classList.add("capturing");
    box.textContent = t("settings_capture_prompt");
    const handler = (e: KeyboardEvent) => {
      e.preventDefault();
      e.stopPropagation();
      if (e.code === "Escape") {
        box.classList.remove("capturing");
        refresh();
        window.removeEventListener("keydown", handler, true);
        return;
      }
      const name = keyName(e);
      if (!name || MODIFIER_ONLY.has(name)) return;
      const mods: string[] = [];
      if (e.ctrlKey) mods.push("Control");
      if (e.altKey) mods.push("Alt");
      if (e.shiftKey) mods.push("Shift");
      if (e.metaKey) mods.push("Win");
      work.hotkey = name;
      work.modifiers = mods.length === 0 ? "None" : mods.join(", ");
      box.classList.remove("capturing");
      refresh();
      renderModifierChips();
      window.removeEventListener("keydown", handler, true);
    };
    window.addEventListener("keydown", handler, true);
  });
}

// —— appearance ——
function initAppearance() {
  const color = $("border-color") as HTMLInputElement;
  const opacity = $("opacity") as HTMLInputElement;
  const multicolor = $("multicolor") as HTMLInputElement;
  color.value = work.color.toLowerCase();
  ($("color-hex") as HTMLElement).textContent = work.color.toUpperCase();
  opacity.value = String(work.opacity);
  ($("opacity-value") as HTMLElement).textContent = String(work.opacity);
  multicolor.checked = work.multicolor;

  color.addEventListener("input", () => {
    work.color = color.value.toUpperCase();
    ($("color-hex") as HTMLElement).textContent = work.color;
  });
  opacity.addEventListener("input", () => {
    work.opacity = Number(opacity.value);
    ($("opacity-value") as HTMLElement).textContent = opacity.value;
  });
  multicolor.addEventListener("change", () => {
    work.multicolor = multicolor.checked;
  });
}

// —— save / cancel ——
function initButtons() {
  $("save").addEventListener("click", async () => {
    if (MODIFIER_ONLY.has(work.hotkey)) {
      alert(t("settings_invalid_key"));
      return;
    }
    await setState.language(work.language);
    await setState.mode(work.mode);
    await setState.targetLanguage(work.target);
    await setState.hotkey(work.hotkey, work.modifiers);
    await setState.appearance(work.color, work.opacity, work.multicolor);
    await hideWindow("settings");
  });
  $("cancel").addEventListener("click", () => void hideWindow("settings"));
}
