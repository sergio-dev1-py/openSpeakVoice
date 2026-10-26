<p align="center">
  <a href="https://github.com/sergio-dev1-py/openSpeakVoice">
    <img src="app/src-tauri/icons/128x128.png" width="96" alt="VoiceToText logo">
  </a>
</p>

<h1 align="center">VoiceToText</h1>

<p align="center">
  <strong>Konuş. Yazıya dök. Çevir. Her yerde.</strong><br>
  Windows, Linux ve macOS için küçük, yüzen bir konuşma-metin kapsülü.
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
  <a href="README.no.md">Norsk</a> |
  <a href="README.br.md">Português (Brasil)</a> |
  <a href="README.th.md">ไทย</a> |
  <strong>Türkçe</strong> |
  <a href="README.uk.md">Українська</a> |
  <a href="README.bn.md">বাংলা</a> |
  <a href="README.gr.md">Ελληνικά</a> |
  <a href="README.vi.md">Tiếng Việt</a>
</p>

<p align="center">
  <img src="docs/preview.png" alt="VoiceToText preview" width="100%">
</p>

## Genel bakış

VoiceToText, Rust + Slint ile yazılmış yerel bir widget'tır: global kısayola basın, konuşun ve tanınan metin etkin uygulamaya yazılsın. Chromium, WebView2 veya Node.js gerekmez.

## Özellikler

- 121 × 32 px yuvarlak kapsül; her zaman üstte, sürüklenebilir ve görev çubuğunda görünmez.
- Global kısayol (varsayılan F8), Groq whisper-large-v3 ve yerel Whisper modelleri.
- Yazıya dökme ve çeviri: openai/gpt-oss-120b, otomatik olarak gpt-oss-20b'ye geçiş.
- İki Groq API anahtarı, otomatik rotasyon ve 93 tanıma dili.
- Kapsül, menü, ayarlar, bildirimler ve sistem tepsisi için açık/koyu tema; 5 dakikaya kadar kayıt.

## İndirme

[Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases/latest)

| Platform | Package |
| --- | --- |
| Windows 10/11 (x64) | `VoiceToText-windows-x64-setup.exe` |
| Linux (x64) | `VoiceToText-linux-x64.deb` / `VoiceToText-linux-x64.AppImage` |
| macOS Apple Silicon | `VoiceToText-macos-arm64.dmg` |
| macOS Intel | `VoiceToText-macos-x64.dmg` |

> Platformunuz için paketi Releases'ten indirin. Yükleyiciler imzasızdır; SmartScreen veya Gatekeeper uyarabilir.

## Hızlı başlangıç

1. Sisteminiz için paketi indirip kurun.
2. VoiceToText'i açın; kapsül son kaydedilen konumda görünür.
3. F8'e basın, konuşun ve tekrar basın; metin etkin uygulamaya eklenir.
4. Sağ tık → API Keys ile Groq'u yapılandırın veya Modelos ile yerel model indirin.

## Kullanım

- Sol tıkla sürükleyin; sağ tık menüyü açar; Esc kapatır.
- Sistem tepsisi simgesi gösterir, gizler veya çıkar.
- Ayarlar, API Keys ve modeller gerektiğinde oluşturulur, kapanınca serbest bırakılır.
- Gizledikten sonra ilk F8 yalnızca kapsülü gösterir; ikinci F8 kaydı başlatır.

## Ayarlar ve veriler

Ayarlar kullanıcı klasöründeki VoiceToTextWidget/settings.json, Whisper modelleri Models klasöründedir. Metin ekleme panoya + Ctrl/Cmd+V ile, olmazsa klavye ile yapılır.

## Kaynaktan derleme

```bash
cd app/src-tauri
cargo build --release
cargo test
```

Rust stable, CMake ve LLVM/libclang gerekir; Linux'ta ayrıca ALSA/X11/Wayland/libxdo, macOS'ta Xcode araçları.

[Tam İngilizce belgeler](README.md) · [Issues](https://github.com/sergio-dev1-py/openSpeakVoice/issues) · [Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases)

## Lisans

MIT © 2026 sergio-dev1-py
