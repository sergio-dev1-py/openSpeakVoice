<p align="center">
  <a href="https://github.com/sergio-dev1-py/openSpeakVoice">
    <img src="app/src-tauri/icons/128x128.png" width="96" alt="VoiceToText logo">
  </a>
</p>

<h1 align="center">VoiceToText</h1>

<p align="center">
  <strong>Govori. Transkribuj. Prevedi. Bilo gdje.</strong><br>
  Mala plutajuća pilula za govor u tekst za Windows, Linux i macOS.
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
  <strong>Bosanski</strong> |
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

## Pregled

VoiceToText je nativni Rust + Slint widget: pritisnite globalnu prečicu, govorite i prepoznati tekst se upisuje u aktivnu aplikaciju. Bez Chromiuma, WebView2 i Node.js.

## Funkcije

- Zaobljena pilula 121 × 32 px, uvijek iznad, pomična i skrivena iz trake zadataka.
- Globalna prečica (F8), Groq whisper-large-v3 i lokalni Whisper modeli.
- Transkripcija i prijevod: openai/gpt-oss-120b uz automatski fallback na gpt-oss-20b.
- Dva Groq API ključa sa rotacijom i 93 jezika prepoznavanja.
- Svijetla/tamna tema pilule, menija, postavki, obavještenja i traya; snimanje do 5 minuta.

## Preuzimanje

[Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases/latest)

| Platform | Package |
| --- | --- |
| Windows 10/11 (x64) | `VoiceToText-windows-x64-setup.exe` |
| Linux (x64) | `VoiceToText-linux-x64.deb` / `VoiceToText-linux-x64.AppImage` |
| macOS Apple Silicon | `VoiceToText-macos-arm64.dmg` |
| macOS Intel | `VoiceToText-macos-x64.dmg` |

> Preuzmite paket za svoju platformu na Releases. Instalateri nisu potpisani; SmartScreen ili Gatekeeper mogu upozoriti.

## Brzi početak

1. Preuzmite i instalirajte paket za svoj sistem.
2. Pokrenite VoiceToText; pilula se pojavljuje na zadnjoj poziciji.
3. Pritisnite F8, govorite i ponovo pritisnite; tekst se upisuje.
4. Desni klik → API Keys za Groq ili Modelos za lokalni model.

## Korištenje

- Prevlačite lijevim klikom; desni otvara meni; Esc ga zatvara.
- Ikona u trayu prikazuje, skriva ili izlazi iz aplikacije.
- Postavke, API Keys i modeli se stvaraju po potrebi i oslobađaju pri zatvaranju.
- Nakon skrivanja prvi F8 samo prikazuje pilulu, drugi počinje snimanje.

## Postavke i podaci

Postavke su u VoiceToTextWidget/settings.json u korisničkom folderu, Whisper modeli u folderu Models. Ubacivanje teksta koristi clipboard + Ctrl/Cmd+V uz fallback tastature.

## Izgradnja iz izvornog koda

```bash
cd app/src-tauri
cargo build --release
cargo test
```

Potrebni su Rust stable, CMake i LLVM/libclang; na Linuxu i ALSA/X11/Wayland/libxdo, na macOS Xcode alati.

[Potpuna dokumentacija na engleskom](README.md) · [Issues](https://github.com/sergio-dev1-py/openSpeakVoice/issues) · [Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases)

## Licenca

MIT © 2026 sergio-dev1-py
