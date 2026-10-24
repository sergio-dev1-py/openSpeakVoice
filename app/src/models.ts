// Whisper models window: list, download with progress, delete, GPU toggle.
import {
  getSettings, getModels, getLanguages, startDownload, cancelDownload,
  deleteModel, setState, hideWindow, listen, type ModelInfo,
} from "./shared/api";
import { makeT } from "./shared/i18n";

let t = makeT("es");
let activeModel = "small";
let useGpu = true;
let downloading: string | null = null;

const $ = <T extends HTMLElement>(id: string) => document.getElementById(id) as T;

const [s, models, langs] = await Promise.all([getSettings(), getModels(), getLanguages()]);
t = makeT(s.AppLanguage);
activeModel = s.LocalModelName;
useGpu = s.UseGpuAcceleration;

$("title").textContent = t("models_title");
$("close").textContent = t("models_close");
for (const el of document.querySelectorAll("[data-i18n]")) {
  el.textContent = t(el.getAttribute("data-i18n")!);
}

const gpuToggle = $("use-gpu") as HTMLInputElement;
gpuToggle.checked = useGpu;
gpuToggle.addEventListener("change", async () => {
  useGpu = gpuToggle.checked;
  await setState.localModel(activeModel, useGpu);
});

render(models);

listen("model-progress", (payload) => {
  const p = payload as { name: string; percent: number };
  if (p.percent >= 100) {
    downloading = null;
    refresh();
    return;
  }
  downloading = p.name;
  const fill = document.querySelector(`[data-fill="${p.name}"]`) as HTMLElement | null;
  if (fill) fill.style.width = `${p.percent}%`;
  const pct = document.querySelector(`[data-pct="${p.name}"]`);
  if (pct) pct.textContent = `${p.percent}%`;
});

listen("model-error", (payload) => {
  const p = payload as { message: string };
  downloading = null;
  refresh();
  if (p.message !== "Descarga cancelada") alert(p.message);
});

listen("models-changed", () => refresh());
listen("settings-changed", (payload) => {
  const p = payload as { settings: { LocalModelName: string } };
  activeModel = p.settings.LocalModelName;
  refresh();
});

async function refresh() {
  render(await getModels());
}

function fmtSize(bytes: number | null): string {
  if (!bytes) return "";
  const mb = bytes / (1024 * 1024);
  return mb > 1024 ? `${(mb / 1024).toFixed(2)} GB` : `${Math.round(mb)} MB`;
}

function render(models: ModelInfo[]) {
  const list = $("model-list");
  list.innerHTML = "";
  for (const m of models) {
    const row = document.createElement("div");
    row.className = "model-row" + (activeModel === m.name ? " active" : "");

    const info = document.createElement("div");
    info.className = "model-info";
    const badges: string[] = [];
    if (activeModel === m.name) badges.push(`<span class="badge active">${t("models_active")}</span>`);
    else if (m.downloaded) badges.push(`<span class="badge ok">${t("models_downloaded")}</span>`);
    info.innerHTML = `
      <div class="model-name">${m.label} ${badges.join(" ")}</div>
      <div class="model-size">${m.size_label}${m.size_on_disk ? ` · ${fmtSize(m.size_on_disk)}` : ""}</div>
      <div class="progress" style="display:${downloading === m.name ? "block" : "none"}">
        <div class="fill" data-fill="${m.name}"></div>
      </div>`;

    const actions = document.createElement("div");
    actions.className = "row";
    actions.style.gap = "6px";

    if (downloading === m.name) {
      const pct = document.createElement("span");
      pct.className = "hint";
      pct.dataset.pct = m.name;
      pct.textContent = "0%";
      const cancel = document.createElement("button");
      cancel.className = "btn";
      cancel.textContent = t("models_cancel");
      cancel.addEventListener("click", () => void cancelDownload());
      actions.appendChild(pct);
      actions.appendChild(cancel);
    } else {
      if (!m.downloaded) {
        const dl = document.createElement("button");
        dl.className = "btn primary";
        dl.textContent = t("models_download");
        dl.addEventListener("click", () => {
          downloading = m.name;
          render(models);
          void startDownload(m.name);
        });
        actions.appendChild(dl);
      } else {
        if (activeModel !== m.name) {
          const use = document.createElement("button");
          use.className = "btn";
          use.textContent = t("models_set_active");
          use.addEventListener("click", () => {
            void setState.localModel(m.name, useGpu);
          });
          actions.appendChild(use);
        }
        const del = document.createElement("button");
        del.className = "btn danger";
        del.textContent = t("models_delete");
        del.addEventListener("click", async () => {
          if (confirm(`${t("models_confirm_delete")} (${m.label})`)) {
            await deleteModel(m.name);
          }
        });
        actions.appendChild(del);
      }
    }

    row.appendChild(info);
    row.appendChild(actions);
    list.appendChild(row);
  }
}

$("close").addEventListener("click", () => void hideWindow("models"));
void langs;
