// Typed wrappers over the Rust command layer. Settings mirror the legacy
// PascalCase settings.json keys exactly.
import { invoke as tauriInvoke } from "@tauri-apps/api/core";
import { listen as tauriListen } from "@tauri-apps/api/event";

export type Provider = "GroqApi" | "LocalWhisper";

export interface AppSettings {
  HotKeyId: number;
  HotKey: string;
  Modifiers: string;
  ApiKey1: string;
  ApiKey2: string;
  ApiKey1RequestCount: number;
  ApiKey2RequestCount: number;
  ApiKey1WindowStart: string;
  ApiKey2WindowStart: string;
  Language: string;
  AppLanguage: string;
  Mode: "transcription" | "translation";
  TargetLanguage: string;
  ActiveProvider: Provider;
  LocalModelName: string;
  UseGpuAcceleration: boolean;
  WhisperPrompt: string;
  BorderColor: string;
  BorderOpacity: number;
  MulticolorBorder: boolean;
  WidgetPosX: number;
  WidgetPosY: number;
}

export interface Usage {
  used1: number;
  total1: number;
  used2: number;
  total2: number;
  hasKey1: boolean;
  hasKey2: boolean;
}

export interface ModelInfo {
  name: string;
  label: string;
  size_label: string;
  downloaded: boolean;
  size_on_disk: number | null;
}

export interface LangEntry {
  code: string;
  name: string;
}

export const invoke = <T>(cmd: string, args?: Record<string, unknown>): Promise<T> =>
  tauriInvoke<T>(cmd, args);

// — Commands —
export const getState = () => invoke<string>("get_state");
export const getSettings = () => invoke<AppSettings>("get_settings");
export const getLanguages = () =>
  invoke<{ common: LangEntry[]; all: LangEntry[] }>("get_languages");
export const setState = {
  language: (code: string) => invoke<AppSettings>("set_language", { code }),
  mode: (mode: string) => invoke<AppSettings>("set_mode", { mode }),
  targetLanguage: (code: string) => invoke<AppSettings>("set_target_language", { code }),
  provider: (provider: string) => invoke<AppSettings>("set_provider", { provider }),
  apiKeys: (key1: string, key2: string) => invoke<AppSettings>("set_api_keys", { key1, key2 }),
  appearance: (color: string, opacity: number, multicolor: boolean) =>
    invoke<AppSettings>("set_appearance", { color, opacity, multicolor }),
  localModel: (name: string, useGpu: boolean) =>
    invoke<AppSettings>("set_local_model", { name, useGpu }),
  hotkey: (key: string, modifiers: string) =>
    invoke<AppSettings>("set_hotkey", { key, modifiers }),
};
export const toggleRecording = () => invoke<void>("toggle_recording");
export const openMenu = () => invoke<void>("open_menu");
export const closeMenu = () => invoke<void>("close_menu");
export const hidePill = () => invoke<void>("hide_pill");
export const showWindow = (label: string) => invoke<void>("show_window", { label });
export const hideWindow = (label: string) => invoke<void>("hide_window", { label });
export const quitApp = () => invoke<void>("quit_app");
export const pillMove = (x: number, y: number) => invoke<void>("pill_move", { x, y });
export const savePillPosition = (x: number, y: number) =>
  invoke<void>("set_widget_position", { x, y });
export const getModels = () => invoke<ModelInfo[]>("get_models");
export const startDownload = (name: string) => invoke<void>("start_download", { name });
export const cancelDownload = () => invoke<void>("cancel_download");
export const deleteModel = (name: string) => invoke<void>("delete_model", { name });
export const getUsage = () => invoke<Usage>("get_usage");

// — Events —
export type Listener = (payload: unknown) => void;

export function listen(event: string, cb: Listener): Promise<() => void> {
  return tauriListen<unknown>(event, (ev) => cb(ev.payload));
}
