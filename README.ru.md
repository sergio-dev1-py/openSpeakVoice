<p align="center">
  <a href="https://github.com/sergio-dev1-py/openSpeakVoice">
    <img src="app/src-tauri/icons/128x128.png" width="96" alt="VoiceToText logo">
  </a>
</p>

<h1 align="center">VoiceToText</h1>

<p align="center">
  <strong>Говорите. Расшифровывайте. Переводите. Где угодно.</strong><br>
  Миниатюрная плавающая капсула распознавания речи для Windows, Linux и macOS.
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
  <strong>Русский</strong> |
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

## Обзор

VoiceToText — нативный виджет на Rust + Slint: нажмите глобальную горячую клавишу, говорите, и распознанный текст вставляется в активное приложение. Без Chromium, WebView2 и Node.js.

## Возможности

- Округлая капсула 121 × 32 px, всегда сверху, перетаскивается и не отображается на панели задач.
- Глобальная горячая клавиша (по умолчанию F8), Groq whisper-large-v3 и локальные модели Whisper.
- Расшифровка и перевод: openai/gpt-oss-120b с автоматическим переходом на gpt-oss-20b.
- Два API-ключа Groq с ротацией и 93 языка распознавания.
- Светлая/тёмная тема для капсулы, меню, настроек, уведомлений и трея; запись до 5 минут.

## Загрузка

[Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases/latest)

| Platform | Package |
| --- | --- |
| Windows 10/11 (x64) | `VoiceToText-windows-x64-setup.exe` |
| Linux (x64) | `VoiceToText-linux-x64.deb` / `VoiceToText-linux-x64.AppImage` |
| macOS Apple Silicon | `VoiceToText-macos-arm64.dmg` |
| macOS Intel | `VoiceToText-macos-x64.dmg` |

> Скачайте пакет для своей платформы в Releases. Установщики не подписаны; SmartScreen или Gatekeeper могут предупредить.

## Быстрый старт

1. Скачайте и установите пакет для вашей системы.
2. Запустите VoiceToText; капсула появится на последней позиции.
3. Нажмите F8, говорите и нажмите снова; текст вставится в активное приложение.
4. Правый клик → API Keys для Groq или Modelos для локальной модели.

## Использование

- Перетаскивайте левой кнопкой; правый клик открывает меню; Esc закрывает.
- Значок в трее позволяет показать, скрыть или выйти.
- Окна настроек, API Keys и моделей создаются по требованию и освобождаются.
- После скрытия первый F8 только показывает капсулу, второй начинает запись.

## Настройки и данные

Настройки хранятся в VoiceToTextWidget/settings.json в папке пользователя, модели Whisper — в папке Models. Вставка текста использует буфер обмена + Ctrl/Cmd+V с запасным вводом с клавиатуры.

## Сборка из исходников

```bash
cd app/src-tauri
cargo build --release
cargo test
```

Нужны Rust stable, CMake и LLVM/libclang; в Linux также ALSA/X11/Wayland/libxdo, в macOS инструменты Xcode.

[Полная документация на английском](README.md) · [Issues](https://github.com/sergio-dev1-py/openSpeakVoice/issues) · [Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases)

## Лицензия

MIT © 2026 sergio-dev1-py
