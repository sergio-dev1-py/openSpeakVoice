<p align="center">
  <a href="https://github.com/sergio-dev1-py/openSpeakVoice">
    <img src="app/src-tauri/icons/128x128.png" width="96" alt="VoiceToText logo">
  </a>
</p>

<h1 align="center">VoiceToText</h1>

<p align="center">
  <strong>说。转写。翻译。随处可用。</strong><br>
  适用于 Windows、Linux 和 macOS 的迷你常驻语音转文字胶囊。
</p>

<p align="center">
  <a href="https://github.com/sergio-dev1-py/openSpeakVoice/releases/latest"><img alt="Latest release" src="https://img.shields.io/github/v/release/sergio-dev1-py/openSpeakVoice?style=flat-square"></a>
  <a href="https://github.com/sergio-dev1-py/openSpeakVoice/actions/workflows/build.yml"><img alt="Build" src="https://img.shields.io/github/actions/workflow/status/sergio-dev1-py/openSpeakVoice/build.yml?branch=main&style=flat-square"></a>
  <a href="LICENSE"><img alt="MIT License" src="https://img.shields.io/badge/license-MIT-blue?style=flat-square"></a>
</p>

<p align="center">
  <a href="README.md">English</a> |
  <strong>简体中文</strong> |
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

VoiceToText 是一款原生 Rust + Slint 桌面小部件：按下全局快捷键，说话，松开后文字会直接输入到当前应用。无需 Chromium、WebView2 或 Node.js。

## 功能

- 121 × 32 像素圆角悬浮胶囊，始终置顶，可拖动，不显示在任务栏。
- 全局快捷键（默认 F8），支持 Groq 云端 whisper-large-v3 和本地 Whisper 模型。
- 转写与翻译：openai/gpt-oss-120b，失败时自动切换 gpt-oss-20b。
- 双 Groq API Key 自动轮换，每种语言 93 种识别语言。
- 明暗主题覆盖胶囊、菜单、设置、提示和托盘；录制上限 5 分钟。

## 下载

[Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases/latest)

| Platform | Package |
| --- | --- |
| Windows 10/11 (x64) | `VoiceToText-windows-x64-setup.exe` |
| Linux (x64) | `VoiceToText-linux-x64.deb` / `VoiceToText-linux-x64.AppImage` |
| macOS Apple Silicon | `VoiceToText-macos-arm64.dmg` |
| macOS Intel | `VoiceToText-macos-x64.dmg` |

> 从 Releases 下载对应安装包。安装包未签名，Windows SmartScreen 或 macOS Gatekeeper 可能提示。

## 快速开始

1. 下载并安装适用于你系统的版本。
2. 启动 VoiceToText，胶囊会出现在上次保存的位置。
3. 按 F8 开始说话，再按一次结束；文字会插入当前应用。
4. 在右键菜单 → API Keys 中配置 Groq，或在 Modelos 中下载本地模型。

## 使用

- 左键拖动胶囊；右键打开菜单；Esc 关闭菜单。
- 托盘图标可显示/隐藏或退出应用。
- 设置、API Keys 和模型窗口按需创建，关闭后释放。
- 第一次隐藏后按 F8 只会重新显示胶囊，第二次才开始录音。

## 设置与数据

设置保存在用户目录下的 VoiceToTextWidget/settings.json；Whisper 模型位于同目录的 Models 文件夹。文本注入使用剪贴板 + Ctrl/Cmd+V，失败时回退到模拟键盘输入。

## 从源码构建

```bash
cd app/src-tauri
cargo build --release
cargo test
```

需要 Rust stable、CMake、LLVM/libclang；Linux 另需 ALSA/X11/Wayland/libxdo，macOS 另需 Xcode 工具。

[完整英文文档](README.md) · [Issues](https://github.com/sergio-dev1-py/openSpeakVoice/issues) · [Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases)

## 许可证

MIT © 2026 sergio-dev1-py
