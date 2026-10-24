// Toast window: error/success notifications anchored to the pill.
import { listen, hideWindow } from "./shared/api";

const toast = document.getElementById("toast") as HTMLDivElement;
const iconEl = document.getElementById("t-icon") as HTMLSpanElement;
const msgEl = document.getElementById("t-msg") as HTMLSpanElement;

const ICON_ERROR = `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/></svg>`;
const ICON_OK = `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>`;

let hideTimer: number | undefined;

listen("toast-data", (payload) => {
  const p = payload as { kind: string; message: string };
  toast.classList.toggle("error", p.kind === "error");
  toast.classList.toggle("success", p.kind === "success");
  iconEl.innerHTML = p.kind === "error" ? ICON_ERROR : ICON_OK;
  msgEl.textContent = p.message;
  if (hideTimer) window.clearTimeout(hideTimer);
  hideTimer = window.setTimeout(() => void hideWindow("toast"), p.kind === "error" ? 5000 : 2500);
});
