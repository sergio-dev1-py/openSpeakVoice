// Groq API keys window: dual keys + usage status.
import { getSettings, getUsage, setState, hideWindow, type Usage } from "./shared/api";
import { makeT } from "./shared/i18n";

let t = makeT("es");

const $ = <T extends HTMLElement>(id: string) => document.getElementById(id) as T;
const key1 = $("key1") as HTMLInputElement;
const key2 = $("key2") as HTMLInputElement;

const [s, usage] = await Promise.all([getSettings(), getUsage()]);
t = makeT(s.AppLanguage);

$("title").textContent = t("keys_title");
$("k1-label").textContent = t("keys_key1");
$("k2-label").textContent = t("keys_key2");
$("info").textContent = t("keys_info");
$("save").textContent = t("keys_save");
$("close").textContent = t("keys_cancel");
key1.value = s.ApiKey1;
key2.value = s.ApiKey2;
renderUsage(usage);

function renderUsage(u: Usage) {
  const fmt = (mask: string, used: number, total: number) =>
    `${mask}  ·  ${t("keys_usage")}: ${used}/${total}`;
  $("usage1").textContent = fmt(
    u.hasKey1 ? `****${s.ApiKey1.slice(-4)}` : t("keys_not_set"),
    u.used1,
    u.total1
  );
  $("usage2").textContent = fmt(
    u.hasKey2 ? `****${s.ApiKey2.slice(-4)}` : t("keys_not_set"),
    u.used2,
    u.total2
  );
}

$("save").addEventListener("click", async () => {
  await setState.apiKeys(key1.value, key2.value);
  await hideWindow("keys");
});

$("close").addEventListener("click", () => void hideWindow("keys"));
