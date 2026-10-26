<p align="center">
  <a href="https://github.com/sergio-dev1-py/openSpeakVoice">
    <img src="app/src-tauri/icons/128x128.png" width="96" alt="VoiceToText logo">
  </a>
</p>

<h1 align="center">VoiceToText</h1>

<p align="center">
  <strong>話す。文字起こし。翻訳。どこでも。</strong><br>
  Windows・Linux・macOS 向けの小さな常駐音声入力ピル。
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
  <strong>日本語</strong> |
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

## 概要

VoiceToText は Rust + Slint のネイティブウィジェットです。グローバルホットキーを押して話すと、認識したテキストが現在のアプリに直接入力されます。Chromium、WebView2、Node.js は不要です。

## 機能

- 121 × 32 px の角丸ピル。常に最前面、ドラッグ可能、タスクバーには表示されません。
- グローバルホットキー（既定 F8）、Groq whisper-large-v3、ローカル Whisper モデルに対応。
- 文字起こしと翻訳：openai/gpt-oss-120b、失敗時は gpt-oss-20b に自動フォールバック。
- 2 つの Groq API キーを自動ローテーション、93 言語の音声認識。
- ピル、メニュー、設定、トースト、トレイのライト/ダークテーマ。録音は最大 5 分。

## ダウンロード

[Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases/latest)

| Platform | Package |
| --- | --- |
| Windows 10/11 (x64) | `VoiceToText-windows-x64-setup.exe` |
| Linux (x64) | `VoiceToText-linux-x64.deb` / `VoiceToText-linux-x64.AppImage` |
| macOS Apple Silicon | `VoiceToText-macos-arm64.dmg` |
| macOS Intel | `VoiceToText-macos-x64.dmg` |

> Releases からお使いの環境用パッケージをダウンロードしてください。インストーラーは未署名のため、SmartScreen や Gatekeeper の警告が出る場合があります。

## クイックスタート

1. お使いの OS 用パッケージをダウンロードしてインストールします。
2. VoiceToText を起動すると、最後に保存した位置にピルが表示されます。
3. F8 を押して話し、もう一度押すとテキストが入力されます。
4. 右クリック → API Keys で Groq を設定、または Modelos でローカルモデルを取得します。

## 使い方

- 左ドラッグで移動、右クリックでメニュー、Esc で閉じます。
- トレイアイコンから表示・非表示・終了ができます。
- 設定、API Keys、モデル画面は必要時に生成され、閉じると解放されます。
- 非表示後の最初の F8 はピルを表示するだけで、次で録音が始まります。

## 設定とデータ

設定はユーザーフォルダーの VoiceToTextWidget/settings.json、Whisper モデルは同じ場所の Models に保存されます。テキスト入力はクリップボード + Ctrl/Cmd+V、失敗時はキーボード入力です。

## ソースからビルド

```bash
cd app/src-tauri
cargo build --release
cargo test
```

Rust stable、CMake、LLVM/libclang が必要です。Linux は ALSA/X11/Wayland/libxdo、macOS は Xcode ツールも必要です。

[英語の完全なドキュメント](README.md) · [Issues](https://github.com/sergio-dev1-py/openSpeakVoice/issues) · [Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases)

## ライセンス

MIT © 2026 sergio-dev1-py
