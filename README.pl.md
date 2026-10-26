<p align="center">
  <a href="https://github.com/sergio-dev1-py/openSpeakVoice">
    <img src="app/src-tauri/icons/128x128.png" width="96" alt="VoiceToText logo">
  </a>
</p>

<h1 align="center">VoiceToText</h1>

<p align="center">
  <strong>Mów. Transkrybuj. Tłumacz. Wszędzie.</strong><br>
  Mała pływająca pigułka mowy na tekst dla Windows, Linux i macOS.
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
  <strong>Polski</strong> |
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

## Przegląd

VoiceToText to natywny widget Rust + Slint: naciśnij globalny skrót, mów, a rozpoznany tekst trafi bezpośrednio do aktywnej aplikacji. Bez Chromium, WebView2 i Node.js.

## Funkcje

- Zaokrąglona pigułka 121 × 32 px, zawsze na wierzchu, przeciągana i ukryta z paska zadań.
- Globalny skrót (domyślnie F8), Groq whisper-large-v3 i lokalne modele Whisper.
- Transkrypcja i tłumaczenie: openai/gpt-oss-120b z automatycznym fallbackiem do gpt-oss-20b.
- Dwa klucze Groq z rotacją i 93 języki rozpoznawania.
- Jasny/ciemny motyw pigułki, menu, ustawień, powiadomień i tray; nagranie do 5 minut.

## Pobieranie

[Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases/latest)

| Platform | Package |
| --- | --- |
| Windows 10/11 (x64) | `VoiceToText-windows-x64-setup.exe` |
| Linux (x64) | `VoiceToText-linux-x64.deb` / `VoiceToText-linux-x64.AppImage` |
| macOS Apple Silicon | `VoiceToText-macos-arm64.dmg` |
| macOS Intel | `VoiceToText-macos-x64.dmg` |

> Pobierz pakiet dla swojej platformy z Releases. Instalatory nie są podpisane; SmartScreen lub Gatekeeper mogą ostrzec.

## Szybki start

1. Pobierz i zainstaluj pakiet dla swojego systemu.
2. Uruchom VoiceToText; pigułka pojawi się w ostatniej pozycji.
3. Naciśnij F8, mów i naciśnij ponownie; tekst zostanie wstawiony.
4. Klik prawy → API Keys dla Groq lub Modelos dla modelu lokalnego.

## Użycie

- Przeciągaj lewym przyciskiem; prawy otwiera menu; Esc je zamyka.
- Ikona tray pozwala pokazać, ukryć lub zakończyć.
- Ustawienia, API Keys i modele tworzone są na żądanie i zwalniane.
- Po ukryciu pierwsze F8 tylko pokazuje pigułkę; drugie zaczyna nagrywanie.

## Ustawienia i dane

Ustawienia są w VoiceToTextWidget/settings.json w folderze użytkownika, modele Whisper w folderze Models. Wstawianie tekstu używa schowka + Ctrl/Cmd+V z fallbackiem klawiatury.

## Budowanie ze źródeł

```bash
cd app/src-tauri
cargo build --release
cargo test
```

Wymaga Rust stable, CMake i LLVM/libclang; Linux dodatkowo ALSA/X11/Wayland/libxdo, macOS narzędzi Xcode.

[Pełna dokumentacja po angielsku](README.md) · [Issues](https://github.com/sergio-dev1-py/openSpeakVoice/issues) · [Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases)

## Licencja

MIT © 2026 sergio-dev1-py
