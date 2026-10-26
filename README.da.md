<p align="center">
  <a href="https://github.com/sergio-dev1-py/openSpeakVoice">
    <img src="app/src-tauri/icons/128x128.png" width="96" alt="VoiceToText logo">
  </a>
</p>

<h1 align="center">VoiceToText</h1>

<p align="center">
  <strong>Tal. Transskribér. Oversæt. Overalt.</strong><br>
  En lille flydende stemme-til-tekst-pille til Windows, Linux og macOS.
</p>

<p align="center">
  <a href="https://github.com/sergio-dev1-py/openSpeakVoice/releases/latest"><img alt="Latest release" src="https://img.shields.io/github/v/release/sergio-dev1-py/openSpeakVoice?style=flat-square"></a>
  <a href="https://github.com/sergio-dev1-py/openSpeakVoice/actions/workflows/build.yml"><img alt="Build" src="https://img.shields.io/github/actions/workflow/status/sergio-dev1-py/openSpeakVoice/build.yml?branch=main&style=flat-square"></a>
  <a href="LICENSE"><img alt="MIT License" src="https://img.shields.io/badge/license-MIT-blue?style=flat-square"></a>
</p>

<p align="center">
  <a href="README.md">English</a> |
  <a href="README.zh.md">简体中文</a> |
  <a href="README.zht.md">繁體中文</a> |
  <a href="README.ko.md">한국어</a> |
  <a href="README.de.md">Deutsch</a> |
  <a href="README.es.md">Español</a> |
  <a href="README.fr.md">Français</a> |
  <a href="README.it.md">Italiano</a> |
  <strong>Dansk</strong> |
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

## Oversigt

VoiceToText er en native Rust + Slint-widget: tryk på den globale genvej, tal, og den genkendte tekst indsættes direkte i det aktive program. Ingen Chromium, WebView2 eller Node.js.

## Funktioner

- 121 × 32 px afrundet pille, altid øverst, kan trækkes og vises ikke i proceslinjen.
- Global genvej (F8 som standard), Groq whisper-large-v3 og lokale Whisper-modeller.
- Transskription og oversættelse med openai/gpt-oss-120b og automatisk fallback til gpt-oss-20b.
- To Groq API-nøgler med rotation og 93 genkendelsessprog.
- Lyst/mørkt tema til pille, menu, indstillinger, toast og proceslinje; optagelse op til 5 minutter.

## Download

[Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases/latest)

| Platform | Package |
| --- | --- |
| Windows 10/11 (x64) | `VoiceToText-windows-x64-setup.exe` |
| Linux (x64) | `VoiceToText-linux-x64.deb` / `VoiceToText-linux-x64.AppImage` |
| macOS Apple Silicon | `VoiceToText-macos-arm64.dmg` |
| macOS Intel | `VoiceToText-macos-x64.dmg` |

> Hent pakken til din platform under Releases. Installationsfilerne er ikke signerede; SmartScreen eller Gatekeeper kan advare.

## Hurtig start

1. Download og installér pakken til dit system.
2. Start VoiceToText; pillen vises ved den sidst gemte position.
3. Tryk F8, tal og tryk igen; teksten indsættes i det aktive program.
4. Højreklik → API Keys for Groq, eller Modelos for en lokal model.

## Brug

- Træk med venstre museknap; højreklik åbner menuen; Esc lukker den.
- Tray-ikonet kan vise, skjule eller afslutte.
- Indstillinger, API Keys og modeller oprettes efter behov og frigives igen.
- Efter skjul viser første F8 kun pillen; andet F8 starter optagelsen.

## Indstillinger og data

Indstillinger gemmes i VoiceToTextWidget/settings.json i brugerbiblioteket; Whisper-modeller i mappen Models. Tekstindsættelse bruger udklipsholder + Ctrl/Cmd+V med tastatur-fallback.

## Byg fra kildekoden

```bash
cd app/src-tauri
cargo build --release
cargo test
```

Kræver Rust stable, CMake og LLVM/libclang; på Linux også ALSA/X11/Wayland/libxdo, på macOS Xcode-værktøjer.

[Fuld engelsk dokumentation](README.md) · [Issues](https://github.com/sergio-dev1-py/openSpeakVoice/issues) · [Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases)

## Licens

MIT © 2026 sergio-dev1-py
