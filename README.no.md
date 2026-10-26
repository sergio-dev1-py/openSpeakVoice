<p align="center">
  <a href="https://github.com/sergio-dev1-py/openSpeakVoice">
    <img src="app/src-tauri/icons/128x128.png" width="96" alt="VoiceToText logo">
  </a>
</p>

<h1 align="center">VoiceToText</h1>

<p align="center">
  <strong>Snakk. Transkriber. Oversett. Overalt.</strong><br>
  En liten flytende tale-til-tekst-pille for Windows, Linux og macOS.
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
  <a href="README.da.md">Dansk</a> |
  <a href="README.ja.md">日本語</a> |
  <a href="README.pl.md">Polski</a> |
  <a href="README.ru.md">Русский</a> |
  <a href="README.bs.md">Bosanski</a> |
  <a href="README.ar.md">العربية</a> |
  <strong>Norsk</strong> |
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

## Oversikt

VoiceToText er en native Rust + Slint-widget: trykk på den globale hurtigtasten, snakk, og teksten settes inn i det aktive programmet. Uten Chromium, WebView2 eller Node.js.

## Funksjoner

- Avrundet pille på 121 × 32 px, alltid øverst, flyttbar og skjult fra oppgavelinjen.
- Global hurtigtast (F8 som standard), Groq whisper-large-v3 og lokale Whisper-modeller.
- Transkripsjon og oversettelse med openai/gpt-oss-120b og automatisk fallback til gpt-oss-20b.
- To Groq API-nøkler med rotasjon og 93 gjenkjennelsesspråk.
- Lyst/mørkt tema for pille, meny, innstillinger, varsler og systemkurv; opptak opptil 5 minutter.

## Nedlasting

[Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases/latest)

| Platform | Package |
| --- | --- |
| Windows 10/11 (x64) | `VoiceToText-windows-x64-setup.exe` |
| Linux (x64) | `VoiceToText-linux-x64.deb` / `VoiceToText-linux-x64.AppImage` |
| macOS Apple Silicon | `VoiceToText-macos-arm64.dmg` |
| macOS Intel | `VoiceToText-macos-x64.dmg` |

> Last ned pakken for plattformen din fra Releases. Installatørene er usignerte; SmartScreen eller Gatekeeper kan varsle.

## Hurtigstart

1. Last ned og installer pakken for systemet ditt.
2. Start VoiceToText; pillen vises på sist lagrede posisjon.
3. Trykk F8, snakk og trykk igjen; teksten settes inn.
4. Høyreklikk → API Keys for Groq, eller Modelos for en lokal modell.

## Bruk

- Dra med venstre museknapp; høyreklikk åpner menyen; Esc lukker den.
- Systemkurv-ikonet viser, skjuler eller avslutter.
- Innstillinger, API Keys og modeller opprettes ved behov og frigjøres.
- Etter skjuling viser første F8 bare pillen; andre starter opptak.

## Innstillinger og data

Innstillinger lagres i VoiceToTextWidget/settings.json i brukermappen; Whisper-modeller i Models-mappen. Tekstinnsetting bruker utklippstavle + Ctrl/Cmd+V med tastatur-fallback.

## Bygg fra kildekoden

```bash
cd app/src-tauri
cargo build --release
cargo test
```

Krever Rust stable, CMake og LLVM/libclang; på Linux også ALSA/X11/Wayland/libxdo, på macOS Xcode-verktøy.

[Full engelsk dokumentasjon](README.md) · [Issues](https://github.com/sergio-dev1-py/openSpeakVoice/issues) · [Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases)

## Lisens

MIT © 2026 sergio-dev1-py
