<p align="center">
  <a href="https://github.com/sergio-dev1-py/openSpeakVoice">
    <img src="app/src-tauri/icons/128x128.png" width="96" alt="VoiceToText logo">
  </a>
</p>

<h1 align="center">VoiceToText</h1>

<p align="center">
  <strong>Speak. Transcribe. Translate. Anywhere.</strong><br>
  A tiny always-on-top voice-to-text pill for Windows, Linux and macOS.
</p>

<p align="center">
  <a href="https://github.com/sergio-dev1-py/openSpeakVoice/releases/latest"><img alt="Latest release" src="https://img.shields.io/github/v/release/sergio-dev1-py/openSpeakVoice?style=flat-square"></a>
  <a href="https://github.com/sergio-dev1-py/openSpeakVoice/actions/workflows/build.yml"><img alt="Build" src="https://img.shields.io/github/actions/workflow/status/sergio-dev1-py/openSpeakVoice/build.yml?branch=main&style=flat-square"></a>
  <a href="LICENSE"><img alt="MIT License" src="https://img.shields.io/badge/license-MIT-blue?style=flat-square"></a>
  <img alt="Windows" src="https://img.shields.io/badge/Windows-10%2F11-0078D4?style=flat-square">
  <img alt="Linux" src="https://img.shields.io/badge/Linux-x86__64-FCC624?style=flat-square">
  <img alt="macOS" src="https://img.shields.io/badge/macOS-12%2B-111111?style=flat-square">
  <img alt="Rust" src="https://img.shields.io/badge/Rust-1.92%2B-000000?style=flat-square&logo=rust">
  <img alt="Slint" src="https://img.shields.io/badge/Slint-1.18-2379F4?style=flat-square">
</p>

<p align="center">
  <strong>English</strong> |
  <a href="README.zh.md">简体中文</a> |
  <a href="README.zht.md">繁體中文</a> |
  <a href="README.ko.md">한국어</a> |
  <a href="README.de.md">Deutsch</a> |
  <a href="README.es.md">Español</a> |
  <a href="README.fr.md">Français</a> |
  <a href="README.it.md">Italiano</a> |
  <a href="README.da.md">Dansk</a> |
  <a href="README.ja.md">日本語</a> |
  <a href="README.pl.md">Polski</a> |
  <a href="README.ru.md">Русский</a> |
  <a href="README.bs.md">Bosanski</a> |
  <a href="README.ar.md">العربية</a> |
  <a href="README.no.md">Norsk</a> |
  <a href="README.br.md">Português (Brasil)</a> |
  <a href="README.th.md">ไทย</a> |
  <a href="README.tr.md">Türkçe</a> |
  <a href="README.uk.md">Українська</a> |
  <a href="README.bn.md">বাংলা</a> |
  <a href="README.gr.md">Ελληνικά</a> |
  <a href="README.vi.md">Tiếng Việt</a>
</p>

<p align="center">
  <img src="docs/preview.png" alt="VoiceToText preview" width="100%">
</p>

## Overview

VoiceToText is a compact desktop widget that turns speech into text without
breaking your flow. It stays as a **121 × 32 px pill** on top of your windows,
listens through a global hotkey and inserts the result directly into the
application you are using.

It is a native **Rust + Slint** application: no Chromium, no WebView2, no
Node.js and no browser process. The UI uses Slint's software renderer, so it
also works on machines without a usable OpenGL context.

## Features

- **Tiny always-on-top pill** — 121 × 32 px, rounded, transparent, draggable
  and hidden from the taskbar.
- **Global hotkey** — press once to record, again to stop and transcribe.
  Fully configurable (F1–F24, letters, digits and modifiers).
- **Two speech providers**
  - **Groq Cloud** with `whisper-large-v3`.
  - **Local Whisper** with `tiny`, `base`, `small`, `medium` or `large-v3`
    ggml models. Models are downloaded from inside the app.
- **Transcription and translation** — translate with
  `openai/gpt-oss-120b` and automatically fall back to
  `openai/gpt-oss-20b`.
- **Dual API keys** — two Groq keys with automatic rotation (19 requests per
  minute per key) and per-minute usage indicators.
- **93 recognition languages** and a separate target language for translation.
- **Light and dark themes** across the pill, menu, settings, API keys, models,
  toast notifications and tray icon.
- **On-demand windows** — settings, menu and toast are created only when
  needed and released afterwards.
- **Five-minute recording limit** with automatic stop and transcription.
- **Native drag** on Windows with saved position.
- **Text injection** through the clipboard (`Ctrl+V` / `Cmd+V`) with a typing
  fallback.
- **System tray** with show/hide/quit actions.
- **Local data** — API keys and settings stay on your machine, never in the
  repository.

## Download

Get the latest installers from the
[**Releases page**](https://github.com/sergio-dev1-py/openSpeakVoice/releases/latest).

| Platform | Package |
| --- | --- |
| Windows 10/11 (x64) | `VoiceToText-windows-x64-setup.exe` (NSIS installer) |
| Linux (x64) | `VoiceToText-linux-x64.deb` or `VoiceToText-linux-x64.AppImage` |
| macOS 12+ (Apple Silicon) | `VoiceToText-macos-arm64.dmg` |
| macOS 12+ (Intel) | `VoiceToText-macos-x64.dmg` |

> [!NOTE]
> The installers are not code-signed. Windows SmartScreen and macOS Gatekeeper
> may show a warning. The source code is public and every package is built by
> the GitHub Actions workflow in this repository.

## Quick start

1. Download and install the package for your platform.
2. Launch **VoiceToText**. The pill appears near the last saved position.
3. Press **F8** (or your configured hotkey), speak, and press it again.
4. The recognized text is inserted into the focused application.

For the cloud provider, open the pill menu → **API Keys…** and add one or two
Groq API keys. For offline use, choose **Whisper (local)** and download a model
from **Modelos…**.

> [!TIP]
> If the pill is not visible, open the Windows notification-area overflow or
> the macOS/Linux tray and click the VoiceToText icon. The first hotkey press
> after hiding the pill only brings it back; the next one starts recording.

## Usage

| Action | How |
| --- | --- |
| Start / stop recording | `F8` or the pill button |
| Move the pill | Left-click and drag |
| Open the context menu | Right-click anywhere on the pill |
| Close the menu | `Escape`, click outside, or focus another window |
| Open settings | Right-click → **Ajustes** |
| Configure keys | Right-click → **API Keys…** |
| Manage local models | Right-click → **Modelos…** |
| Show/hide | Tray icon → **Mostrar / Ocultar** |
| Quit | Pill menu or tray → **Salir** |

## How it works

```text
Global hotkey / pill
        │
        ▼
cpal microphone  ──►  16 kHz mono PCM
        │
        ├──► Groq: whisper-large-v3
        └──► Local: whisper.cpp (ggml)
                        │
                        ▼
              optional translation
            openai/gpt-oss-120b → 20b
                        │
                        ▼
       clipboard + Ctrl/Cmd+V → focused app
```

The UI state is polled by a single lightweight timer. While idle the wave is
static and the timer runs at 10 Hz; while recording it rises to 30 Hz. All
windows are Slint components; only the pill is created at startup.

## Configuration

Settings are stored outside the repository:

| OS | Location |
| --- | --- |
| Windows | `%LOCALAPPDATA%\VoiceToTextWidget\settings.json` |
| Linux | `~/.local/share/VoiceToTextWidget/settings.json` |
| macOS | `~/Library/Application Support/VoiceToTextWidget/settings.json` |

The format is compatible with the previous C#/Tauri versions, so existing API
keys, preferences and downloaded Whisper models are reused. Models live in the
`Models` folder next to `settings.json`; logs are written to `app.log`.

### API keys

VoiceToText never ships with API keys. Add your own Groq keys in
**API Keys…**. Keys are stored locally in `settings.json` and are used only to
call the Groq API directly from your machine.

## Build from source

### Requirements

| Platform | Requirements |
| --- | --- |
| Windows | Rust stable (MSVC), Visual Studio Build Tools, CMake, LLVM/libclang |
| Linux | Rust stable, ALSA, X11/Wayland, libxdo, CMake, libclang |
| macOS | Rust stable, Xcode Command Line Tools, CMake, LLVM/libclang |

`whisper-rs` uses `bindgen`, which requires `libclang`. On Windows the helper
script below can configure the local toolchain automatically.

```bat
cd app\src-tauri
set "LIBCLANG_PATH=C:\path\to\llvm\bin"
cargo build --release
cargo test
```

The executable is written to `app/src-tauri/target/release/voicetotext.exe`.

Alternatively, run `app\iniciar-app.bat`: it configures the local
`LIBCLANG_PATH`/CMake paths, updates the release if needed and opens it.

> [!WARNING]
> Do not build the release with `-C lto=off`: this workspace uses `lto = "fat"`
> and mixing a non-LTO binary produces `LNK2001`/`LNK1136` errors. For quick
> iterations use the debug profile.

### Installers

Packaging is done with [cargo-packager](https://github.com/crabnebula-dev/cargo-packager):

```bash
cargo install cargo-packager --locked
cd app/src-tauri

# Windows
cargo packager --release --formats nsis

# Linux
cargo packager --release --formats deb,appimage

# macOS
cargo packager --release --formats app,dmg
```

The CI workflow builds the same packages on native runners and attaches them
to a GitHub release.

## Project structure

```text
app/
├─ iniciar-app.bat             # local Windows launcher
└─ src-tauri/
   ├─ Cargo.toml
   ├─ build.rs                 # compiles the Slint UI
   ├─ ui/app.slint             # pill, menu, settings, toast, tray
   ├─ icons/                   # app, tray and bolt icons
   └─ src/
      ├─ lib.rs                # Slint wiring, windows, hotkey, tray, drag
      ├─ core.rs               # state, recording, STT, translation
      ├─ audio.rs              # cpal capture + 16 kHz resampling
      ├─ whisper.rs            # local ggml models + whisper.cpp
      ├─ groq.rs               # Groq STT + translation + key rotation
      ├─ inject.rs             # clipboard/Ctrl+V injection
      ├─ hotkey.rs             # C#/W3C key-name mapping
      ├─ languages.rs          # language tables
      ├─ settings.rs           # compatible settings.json
      └─ logger.rs             # app.log
```

## Performance

Measured on the reference Windows machine (release, idle):

- **One process**, no WebView2/Chromium helpers.
- **~16–20 MB** working set in idle; ~22 MB with settings open.
- **~0.1 % of one CPU core** in idle, with a static waveform.
- Release executable: **~15 MB**.

## Limitations

- Installers are currently **unsigned**; SmartScreen/Gatekeeper warnings are
  expected until code-signing certificates are configured.
- Linux and macOS packages are built by CI on their native runners; they are
  not compiled from the Windows development machine.
- Dragging above an **elevated** Task Manager can be restricted by Windows UIPI
  when VoiceToText runs at medium integrity. Run both at the same elevation if
  you need to drag the pill while Task Manager is foreground.
- Local Whisper GPU acceleration depends on how `whisper-rs` was compiled; the
  checkbox stores the preference.
- On macOS, microphone and accessibility permissions must be granted on first
  use.

## Contributing

Issues and pull requests are welcome. Please keep changes focused, run
`cargo fmt`, `cargo clippy` and `cargo test`, and describe the platform you
tested on. For UI changes, include a screenshot or a short recording.

## License

Released under the [MIT License](LICENSE).

<p align="center">
  Made with Rust, Slint and a very small pill.<br>
  If VoiceToText saves you time, consider leaving a star ⭐
</p>
