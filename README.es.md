<p align="center">
  <a href="https://github.com/sergio-dev1-py/openSpeakVoice">
    <img src="app/src-tauri/icons/128x128.png" width="96" alt="VoiceToText logo">
  </a>
</p>

<h1 align="center">VoiceToText</h1>

<p align="center">
  <strong>Habla. Transcribe. Traduce. En cualquier lugar.</strong><br>
  Una píldora flotante de voz a texto para Windows, Linux y macOS.
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
  <strong>Español</strong> |
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

## Descripción

VoiceToText es un widget nativo en Rust + Slint: pulsa el atajo global, habla y el texto reconocido se inserta directamente en la aplicación activa. Sin Chromium, WebView2 ni Node.js.

## Características

- Píldora redondeada de 121 × 32 px, siempre encima, arrastrable y oculta de la barra de tareas.
- Atajo global (F8 por defecto), Groq whisper-large-v3 y modelos Whisper locales.
- Transcripción y traducción con openai/gpt-oss-120b y fallback automático a gpt-oss-20b.
- Dos API keys de Groq con rotación automática y 93 idiomas de reconocimiento.
- Temas claro/oscuro en píldora, menú, ajustes, toasts y bandeja; grabación de hasta 5 minutos.

## Descarga

[Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases/latest)

| Platform | Package |
| --- | --- |
| Windows 10/11 (x64) | `VoiceToText-windows-x64-setup.exe` |
| Linux (x64) | `VoiceToText-linux-x64.deb` / `VoiceToText-linux-x64.AppImage` |
| macOS Apple Silicon | `VoiceToText-macos-arm64.dmg` |
| macOS Intel | `VoiceToText-macos-x64.dmg` |

> Descarga el paquete de tu plataforma desde Releases. Los instaladores no están firmados; SmartScreen o Gatekeeper pueden avisar.

## Inicio rápido

1. Descarga e instala el paquete de tu sistema.
2. Abre VoiceToText; la píldora aparece en la última posición guardada.
3. Pulsa F8, habla y vuelve a pulsar; el texto se inserta en la app activa.
4. Clic derecho → API Keys para Groq, o Modelos para descargar un modelo local.

## Uso

- Arrastra con clic izquierdo; clic derecho abre el menú; Esc lo cierra.
- Desde la bandeja puedes mostrar, ocultar o salir.
- Ajustes, API Keys y Modelos se crean bajo demanda y se liberan al cerrar.
- Tras ocultar la píldora, el primer F8 solo la muestra; el segundo empieza a grabar.

## Ajustes y datos

Los ajustes se guardan en VoiceToTextWidget/settings.json dentro de tu carpeta de usuario; los modelos Whisper, en la carpeta Models. La inyección usa portapapeles + Ctrl/Cmd+V con fallback de teclado.

## Compilar desde el código

```bash
cd app/src-tauri
cargo build --release
cargo test
```

Requiere Rust stable, CMake y LLVM/libclang; en Linux también ALSA/X11/Wayland/libxdo y en macOS las herramientas de Xcode.

[Documentación completa en inglés](README.md) · [Issues](https://github.com/sergio-dev1-py/openSpeakVoice/issues) · [Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases)

## Licencia

MIT © 2026 sergio-dev1-py
