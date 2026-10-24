// Pill window: 120×35 widget with waveform, drag-to-move and click-to-menu.
import { listen, openMenu, pillMove, savePillPosition, getSettings, type AppSettings } from "./shared/api";
import { getCurrentWindow } from "@tauri-apps/api/window";

type PillState = "idle" | "listening" | "transcribing";

const pill = document.getElementById("pill") as HTMLDivElement;
const canvas = document.getElementById("wave") as HTMLCanvasElement;
const ctx = canvas.getContext("2d")!;

const BAR_COUNT = 16;
const bars: number[] = new Array(BAR_COUNT).fill(0.06);
let targetLevel = 0;
let state: PillState = "idle";
let accent = "#ffffff";
let hue = 0;
let multicolor = false;

function applyAccent(s: AppSettings) {
  accent = s.BorderColor || "#ffffff";
  multicolor = s.MulticolorBorder;
  const o = s.BorderOpacity / 255;
  pill.style.setProperty("--pill-accent", withAlpha(accent, o));
  pill.style.setProperty("--pill-accent-glow", withAlpha(accent, 0.35 * o));
}

function withAlpha(hex: string, alpha: number): string {
  const h = hex.replace("#", "");
  if (h.length !== 6) return hex;
  const r = parseInt(h.slice(0, 2), 16);
  const g = parseInt(h.slice(2, 4), 16);
  const b = parseInt(h.slice(4, 6), 16);
  return `rgba(${r},${g},${b},${alpha})`;
}

// —— rendering ——
function draw() {
  requestAnimationFrame(draw);

  for (let i = 0; i < BAR_COUNT; i++) {
    const mid = (BAR_COUNT - 1) / 2;
    const shape = Math.cos((Math.abs(i - mid) / mid) * (Math.PI / 3.2));
    let target: number;
    if (state === "listening") {
      target = Math.max(0.06, targetLevel * shape + 0.05);
    } else if (state === "transcribing") {
      const t = performance.now() / 220;
      target = 0.18 + 0.72 * Math.abs(Math.sin(t + i * 0.55)) * shape;
    } else {
      target = 0.06 * shape + 0.02;
    }
    bars[i] += (target - bars[i]) * (state === "listening" ? 0.55 : 0.14);
  }

  const w = canvas.width;
  const h = canvas.height;
  ctx.clearRect(0, 0, w, h);

  let color: string;
  if (multicolor) {
    hue = (hue + 1.2) % 360;
    color = `hsla(${hue}, 90%, 65%, 0.95)`;
  } else if (state === "idle") {
    color = "rgba(214,218,227,0.55)";
  } else {
    color = "rgba(255,255,255,0.9)";
  }

  const barW = 3;
  const gap = (w - BAR_COUNT * barW) / (BAR_COUNT - 1);
  ctx.fillStyle = color;
  for (let i = 0; i < BAR_COUNT; i++) {
    const bh = Math.max(2, bars[i] * h);
    const x = i * (barW + gap);
    const y = (h - bh) / 2;
    roundRect(x, y, barW, bh, 1.5);
  }
}

function roundRect(x: number, y: number, w: number, h: number, r: number) {
  ctx.beginPath();
  ctx.moveTo(x + r, y);
  ctx.arcTo(x + w, y, x + w, y + h, r);
  ctx.arcTo(x + w, y + h, x, y + h, r);
  ctx.arcTo(x, y + h, x, y, r);
  ctx.arcTo(x, y, x + w, y, r);
  ctx.closePath();
  ctx.fill();
}

requestAnimationFrame(draw);

// —— events from Rust ——
listen("state-changed", (payload) => {
  const p = payload as { state: PillState };
  state = p.state;
  pill.classList.toggle("is-listening", state === "listening");
  pill.classList.toggle("is-transcribing", state === "transcribing");
  if (state !== "listening") targetLevel = 0;
});

listen("audio-level", (payload) => {
  const p = payload as { level: number };
  targetLevel = Math.min(1, Math.max(0.12, p.level * 2.6));
});

listen("settings-changed", (payload) => {
  const p = payload as { settings: AppSettings };
  applyAccent(p.settings);
});

listen("result", () => {
  pill.classList.add("is-transcribing");
  setTimeout(() => pill.classList.remove("is-transcribing"), 450);
});

// —— interactions: click → menu, drag → move ——
let downAt = 0;
let downX = 0;
let downY = 0;
let moved = false;
let dragging = false;
// physical-px base position of the window, resolved at drag start
let base: { x: number; y: number } | null = null;

pill.addEventListener("pointerdown", (e) => {
  downAt = performance.now();
  downX = e.screenX;
  downY = e.screenY;
  moved = false;
  dragging = true;
  base = null;
  getCurrentWindow()
    .outerPosition()
    .then((p) => {
      base = { x: p.x, y: p.y };
    })
    .catch(() => {});
});

pill.addEventListener("pointermove", (e) => {
  if (!dragging) return;
  const dx = e.screenX - downX;
  const dy = e.screenY - downY;
  if (!moved && Math.hypot(dx, dy) > 6) moved = true;
  if (moved && base) {
    void pillMove(base.x + dx, base.y + dy);
  }
});

pill.addEventListener("pointerup", () => {
  dragging = false;
  if (!moved && performance.now() - downAt < 600) {
    void openMenu();
  } else if (moved) {
    getCurrentWindow()
      .outerPosition()
      .then((p) => savePillPosition(p.x, p.y))
      .catch(() => {});
  }
});

pill.addEventListener("contextmenu", (e) => e.preventDefault());

getSettings().then(applyAccent).catch(() => {});
