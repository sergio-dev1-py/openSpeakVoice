<p align="center">
  <a href="https://github.com/sergio-dev1-py/openSpeakVoice">
    <img src="app/src-tauri/icons/128x128.png" width="96" alt="VoiceToText logo">
  </a>
</p>

<h1 align="center">VoiceToText</h1>

<p align="center">
  <strong>Nói. Ghi âm. Dịch. Ở mọi nơi.</strong><br>
  Một viên thuốc chuyển giọng nói thành văn bản nhỏ gọn cho Windows, Linux và macOS.
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
  <a href="README.tr.md">Türkçe</a> |
  <a href="README.uk.md">Українська</a> |
  <a href="README.bn.md">বাংলা</a> |
  <a href="README.gr.md">Ελληνικά</a> |
  <strong>Tiếng Việt</strong>
</p>

<p align="center">
  <img src="docs/preview.png" alt="VoiceToText preview" width="100%">
</p>

## Tổng quan

VoiceToText là widget gốc Rust + Slint: nhấn phím tắt toàn cục, nói và văn bản nhận dạng được chèn trực tiếp vào ứng dụng đang hoạt động. Không cần Chromium, WebView2 hay Node.js.

## Tính năng

- Viên thuốc bo tròn 121 × 32 px, luôn ở trên, kéo được và ẩn khỏi thanh tác vụ.
- Phím tắt toàn cục (mặc định F8), Groq whisper-large-v3 và mô hình Whisper cục bộ.
- Ghi âm và dịch với openai/gpt-oss-120b, tự động chuyển sang gpt-oss-20b khi lỗi.
- Hai khóa Groq tự động luân phiên và 93 ngôn ngữ nhận dạng.
- Giao diện sáng/tối cho viên thuốc, menu, cài đặt, thông báo và khay hệ thống; ghi tối đa 5 phút.

## Tải xuống

[Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases/latest)

| Platform | Package |
| --- | --- |
| Windows 10/11 (x64) | `VoiceToText-windows-x64-setup.exe` |
| Linux (x64) | `VoiceToText-linux-x64.deb` / `VoiceToText-linux-x64.AppImage` |
| macOS Apple Silicon | `VoiceToText-macos-arm64.dmg` |
| macOS Intel | `VoiceToText-macos-x64.dmg` |

> Tải gói cho nền tảng của bạn từ Releases. Bộ cài chưa được ký; SmartScreen hoặc Gatekeeper có thể cảnh báo.

## Bắt đầu nhanh

1. Tải và cài gói cho hệ thống của bạn.
2. Mở VoiceToText; viên thuốc xuất hiện ở vị trí đã lưu.
3. Nhấn F8, nói và nhấn lại; văn bản được chèn vào ứng dụng đang dùng.
4. Chuột phải → API Keys để cấu hình Groq hoặc Modelos để tải mô hình cục bộ.

## Cách dùng

- Kéo bằng chuột trái; chuột phải mở menu; Esc đóng menu.
- Biểu tượng khay hệ thống để hiện, ẩn hoặc thoát.
- Cửa sổ cài đặt, API Keys và mô hình được tạo khi cần và giải phóng khi đóng.
- Sau khi ẩn, F8 đầu tiên chỉ hiện viên thuốc; F8 thứ hai bắt đầu ghi âm.

## Cài đặt và dữ liệu

Cài đặt nằm trong VoiceToTextWidget/settings.json trong thư mục người dùng; mô hình Whisper trong thư mục Models. Chèn văn bản dùng clipboard + Ctrl/Cmd+V, dự phòng bằng bàn phím.

## Xây dựng từ mã nguồn

```bash
cd app/src-tauri
cargo build --release
cargo test
```

Cần Rust stable, CMake và LLVM/libclang; Linux cần thêm ALSA/X11/Wayland/libxdo, macOS cần Xcode tools.

[Tài liệu tiếng Anh đầy đủ](README.md) · [Issues](https://github.com/sergio-dev1-py/openSpeakVoice/issues) · [Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases)

## Giấy phép

MIT © 2026 sergio-dev1-py
