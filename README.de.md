<p align="center">
  <a href="https://github.com/sergio-dev1-py/openSpeakVoice">
    <img src="app/src-tauri/icons/128x128.png" width="96" alt="VoiceToText logo">
  </a>
</p>

<h1 align="center">VoiceToText</h1>

<p align="center">
  <strong>Sprechen. Transkribieren. Übersetzen. Überall.</strong><br>
  Eine winzige, immer sichtbare Sprach-zu-Text-Pille für Windows, Linux und macOS.
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
  <strong>Deutsch</strong> |
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

## Überblick

VoiceToText ist ein natives Rust-+-Slint-Widget: Hotkey drücken, sprechen, und der erkannte Text wird direkt in die aktive Anwendung eingefügt. Kein Chromium, WebView2 oder Node.js.

## Funktionen

- 121 × 32 px große, abgerundete Pille: immer im Vordergrund, verschiebbar, nicht in der Taskleiste.
- Globaler Hotkey (Standard F8), Groq whisper-large-v3 und lokale Whisper-Modelle.
- Transkription und Übersetzung mit openai/gpt-oss-120b, automatischer Fallback auf gpt-oss-20b.
- Zwei Groq-API-Keys mit Rotation und 93 Erkennungssprachen.
- Helles/dunkles Theme für Pille, Menü, Einstellungen, Toasts und Tray; Aufnahme bis 5 Minuten.

## Download

[Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases/latest)

| Platform | Package |
| --- | --- |
| Windows 10/11 (x64) | `VoiceToText-windows-x64-setup.exe` |
| Linux (x64) | `VoiceToText-linux-x64.deb` / `VoiceToText-linux-x64.AppImage` |
| macOS Apple Silicon | `VoiceToText-macos-arm64.dmg` |
| macOS Intel | `VoiceToText-macos-x64.dmg` |

> Lade das passende Paket von den Releases. Die Installer sind nicht signiert; SmartScreen oder Gatekeeper können warnen.

## Schnellstart

1. Das Paket für dein System herunterladen und installieren.
2. VoiceToText starten; die Pille erscheint an der letzten Position.
3. F8 zum Sprechen, erneut F8 zum Beenden; der Text wird eingefügt.
4. Rechtsklick → API Keys für Groq oder Modelos für lokale Modelle.

## Verwendung

- Links ziehen bewegt die Pille; Rechtsklick öffnet das Menü; Esc schließt es.
- Tray-Symbol: anzeigen, ausblenden oder beenden.
- Einstellungen, API Keys und Modelle werden bei Bedarf erstellt und danach freigegeben.
- Nach dem Ausblenden zeigt das erste F8 nur die Pille; das zweite startet die Aufnahme.

## Einstellungen und Daten

Einstellungen liegen unter VoiceToTextWidget/settings.json im Benutzerverzeichnis; Whisper-Modelle im Ordner Models daneben. Texteinfügung nutzt Zwischenablage + Ctrl/Cmd+V mit Tastatur-Fallback.

## Aus dem Quellcode bauen

```bash
cd app/src-tauri
cargo build --release
cargo test
```

Benötigt Rust stable, CMake und LLVM/libclang; unter Linux zusätzlich ALSA/X11/Wayland/libxdo, unter macOS die Xcode-Tools.

[Vollständige englische Dokumentation](README.md) · [Issues](https://github.com/sergio-dev1-py/openSpeakVoice/issues) · [Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases)

## Lizenz

MIT © 2026 sergio-dev1-py
