<p align="center">
  <a href="https://github.com/sergio-dev1-py/openSpeakVoice">
    <img src="app/src-tauri/icons/128x128.png" width="96" alt="VoiceToText logo">
  </a>
</p>

<h1 align="center">VoiceToText</h1>

<p align="center">
  <strong>說。轉寫。翻譯。隨處可用。</strong><br>
  適用於 Windows、Linux 與 macOS 的迷你常駐語音轉文字膠囊。
</p>

<p align="center">
  <a href="https://github.com/sergio-dev1-py/openSpeakVoice/releases/latest"><img alt="Latest release" src="https://img.shields.io/github/v/release/sergio-dev1-py/openSpeakVoice?style=flat-square"></a>
  <a href="https://github.com/sergio-dev1-py/openSpeakVoice/actions/workflows/build.yml"><img alt="Build" src="https://img.shields.io/github/actions/workflow/status/sergio-dev1-py/openSpeakVoice/build.yml?branch=main&style=flat-square"></a>
  <a href="LICENSE"><img alt="MIT License" src="https://img.shields.io/badge/license-MIT-blue?style=flat-square"></a>
</p>

<p align="center">
  <a href="README.md">English</a> |
  <a href="README.zh.md">简体中文</a> |
  <strong>繁體中文</strong> |
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

## 概述

VoiceToText 是原生 Rust + Slint 桌面小工具：按下全域快捷鍵、說話，文字會直接輸入到目前應用程式。不需要 Chromium、WebView2 或 Node.js。

## 功能

- 121 × 32 像素圓角懸浮膠囊，始終置頂、可拖曳、不顯示於工作列。
- 全域快捷鍵（預設 F8），支援 Groq 雲端 whisper-large-v3 與本機 Whisper 模型。
- 轉寫與翻譯：openai/gpt-oss-120b，失敗時自動改用 gpt-oss-20b。
- 雙 Groq API Key 自動輪替，支援 93 種辨識語言。
- 明暗主題覆蓋膠囊、選單、設定、提示與系統匣；錄音上限 5 分鐘。

## 下載

[Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases/latest)

| Platform | Package |
| --- | --- |
| Windows 10/11 (x64) | `VoiceToText-windows-x64-setup.exe` |
| Linux (x64) | `VoiceToText-linux-x64.deb` / `VoiceToText-linux-x64.AppImage` |
| macOS Apple Silicon | `VoiceToText-macos-arm64.dmg` |
| macOS Intel | `VoiceToText-macos-x64.dmg` |

> 從 Releases 下載對應安裝檔。安裝檔未簽章，Windows SmartScreen 或 macOS Gatekeeper 可能顯示警告。

## 快速開始

1. 下載並安裝適合你系統的版本。
2. 啟動 VoiceToText，膠囊會出現在上次儲存的位置。
3. 按 F8 開始說話，再按一次結束；文字會插入目前應用程式。
4. 在右鍵選單 → API Keys 設定 Groq，或在 Modelos 下載本機模型。

## 使用方式

- 左鍵拖曳膠囊；右鍵開啟選單；Esc 關閉選單。
- 系統匣圖示可顯示/隱藏或退出應用程式。
- 設定、API Keys 與模型視窗按需建立，關閉後釋放。
- 隱藏後第一次按 F8 只會顯示膠囊，第二次才開始錄音。

## 設定與資料

設定保存在使用者目錄的 VoiceToTextWidget/settings.json；Whisper 模型位於同目錄的 Models 資料夾。文字注入使用剪貼簿 + Ctrl/Cmd+V，失敗時改用模擬鍵盤。

## 從原始碼建置

```bash
cd app/src-tauri
cargo build --release
cargo test
```

需要 Rust stable、CMake、LLVM/libclang；Linux 另需 ALSA/X11/Wayland/libxdo，macOS 另需 Xcode 工具。

[完整英文文件](README.md) · [Issues](https://github.com/sergio-dev1-py/openSpeakVoice/issues) · [Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases)

## 授權

MIT © 2026 sergio-dev1-py
