# VoiceToText Widget

Píldora flotante de voz-a-texto multiplataforma (**Windows / Linux / macOS**) escrita en **Rust + TypeScript** (Tauri 2).

Pulsa el atajo global (por defecto **F8**), habla, vuelve a pulsar y el texto se transcribe e inserta automáticamente donde tengas el cursor.

![píldora](app/src-tauri/icons/128x128.png)

## Características

- **Píldora 120×35 px** siempre visible: icono de rayo monocromático + forma de onda en vivo (3 estados: reposo / escuchando / transcribiendo).
- **Dos proveedores de reconocimiento:**
  - **Groq (nube)** — `whisper-large-v3`, gratis con cuenta. Doble API key con rotación automática (19 req/min por key).
  - **Whisper (local)** — modelos ggml (tiny/base/small/medium/large-v3) descargados y ejecutados sin conexión vía whisper.cpp.
- **Modos:** transcripción y **traducción** (Groq traduce con `llama-3.3-70b` a cualquiera de los idiomas destino).
- **93 idiomas** de reconocimiento (20 frecuentes + buscador).
- **Atajo global configurable** (F1–F24, letras, dígitos + Ctrl/Alt/Shift/Win).
- **Inyección de texto** por portapapeles + Ctrl+V (Cmd+V en macOS) con fallback de tipeo.
- **Bandeja del sistema** con icono adaptativo al tema.
- Ajustes completos: idioma, modo, atajo, color/opacidad/multicolor del borde, modelos.
- **settings.json compatible con la versión C#** (en Windows reutiliza `%LOCALAPPDATA%\VoiceToTextWidget` y los modelos ya descargados).

## Requisitos por plataforma

| Plataforma | Requisitos |
|---|---|
| Windows 10/11 | WebView2 (preinstalado) — nada más |
| Linux | `webkit2gtk-4.1`, `libasound2` (instalados por el paquete `.deb`; en AppImage pueden requerir libs del sistema) |
| macOS 12+ | Permisos de *Accesibilidad* y *Micrófono* (el SO los pedirá al primer uso) |

## Desarrollo

```bash
cd app
npm install
npm run tauri dev     # desarrollo con recarga
npm run tauri build   # binarios + instaladores
npm test              # (cargo test se ejecuta dentro de tauri build)
```

Requisitos de compilación: **Rust stable**, **Node 20+** y en Windows **VS Build Tools (C++)** + CMake.

## Estructura

```
app/
├─ src/            # Frontend TypeScript (píldora, menú, ajustes, keys, modelos, toast)
├─ src-tauri/
│  ├─ src/         # Backend Rust (audio, whisper, groq, inyección, atajos, bandeja)
│  └─ icons/       # Iconos del rayo monocromático
└─ scripts/        # Generación de iconos
```

## Ejecutables (CI)

Al publicar un tag `v*`, GitHub Actions compila automáticamente:

- Windows: `.exe` (NSIS) y `.msi`
- Linux: `.AppImage` y `.deb`
- macOS: `.dmg` (Apple Silicon e Intel)

## Historial

- **v2.0.0** — Reescritura multiplataforma en Rust + TypeScript. Nuevo diseño, píldora 120×35, icono de rayo monocromático. (La versión original C#/WinForms 1.4.0 se conserva en `VoiceToTextWidget/` como referencia.)
