<p align="center">
  <a href="https://github.com/sergio-dev1-py/openSpeakVoice">
    <img src="app/src-tauri/icons/128x128.png" width="96" alt="VoiceToText logo">
  </a>
</p>

<h1 align="center">VoiceToText</h1>

<p align="center">
  <strong>Parla. Trascrivi. Traduci. Ovunque.</strong><br>
  Una minuscola pillola flottante voce-testo per Windows, Linux e macOS.
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
  <strong>Italiano</strong> |
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

## Panoramica

VoiceToText è un widget nativo Rust + Slint: premi la scorciatoia globale, parla e il testo riconosciuto viene inserito nell'app attiva. Senza Chromium, WebView2 o Node.js.

## Funzionalità

- Pillola arrotondata 121 × 32 px, sempre in primo piano, trascinabile e nascosta dalla barra delle applicazioni.
- Scorciatoia globale (F8 predefinito), Groq whisper-large-v3 e modelli Whisper locali.
- Trascrizione e traduzione con openai/gpt-oss-120b, fallback automatico a gpt-oss-20b.
- Due API key Groq con rotazione automatica e 93 lingue di riconoscimento.
- Temi chiaro/scuro per pillola, menu, impostazioni, toast e tray; registrazione fino a 5 minuti.

## Download

[Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases/latest)

| Platform | Package |
| --- | --- |
| Windows 10/11 (x64) | `VoiceToText-windows-x64-setup.exe` |
| Linux (x64) | `VoiceToText-linux-x64.deb` / `VoiceToText-linux-x64.AppImage` |
| macOS Apple Silicon | `VoiceToText-macos-arm64.dmg` |
| macOS Intel | `VoiceToText-macos-x64.dmg` |

> Scarica il pacchetto della tua piattaforma da Releases. Gli installer non sono firmati; SmartScreen o Gatekeeper possono avvisare.

## Avvio rapido

1. Scarica e installa il pacchetto per il tuo sistema.
2. Avvia VoiceToText; la pillola appare nell'ultima posizione salvata.
3. Premi F8, parla e premi di nuovo; il testo viene inserito.
4. Clic destro → API Keys per Groq, oppure Modelos per un modello locale.

## Utilizzo

- Trascina con il tasto sinistro; clic destro apre il menu; Esc lo chiude.
- L'icona nel tray permette di mostrare, nascondere o uscire.
- Impostazioni, API Keys e Modelli vengono creati su richiesta e rilasciati alla chiusura.
- Dopo aver nascosto la pillola, il primo F8 la mostra soltanto; il secondo avvia la registrazione.

## Impostazioni e dati

Le impostazioni sono in VoiceToTextWidget/settings.json nella cartella utente; i modelli Whisper nella cartella Models. L'inserimento usa appunti + Ctrl/Cmd+V con fallback da tastiera.

## Compila dai sorgenti

```bash
cd app/src-tauri
cargo build --release
cargo test
```

Richiede Rust stable, CMake e LLVM/libclang; su Linux anche ALSA/X11/Wayland/libxdo, su macOS gli strumenti Xcode.

[Documentazione completa in inglese](README.md) · [Issues](https://github.com/sergio-dev1-py/openSpeakVoice/issues) · [Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases)

## Licenza

MIT © 2026 sergio-dev1-py
