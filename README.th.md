<p align="center">
  <a href="https://github.com/sergio-dev1-py/openSpeakVoice">
    <img src="app/src-tauri/icons/128x128.png" width="96" alt="VoiceToText logo">
  </a>
</p>

<h1 align="center">VoiceToText</h1>

<p align="center">
  <strong>พูด ถอดเสียง แปลได้ทุกที่</strong><br>
  แผงลอยแปลงเสียงเป็นข้อความขนาดเล็กสำหรับ Windows, Linux และ macOS
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
  <strong>ไทย</strong> |
  <a href="README.tr.md">Türkçe</a> |
  <a href="README.uk.md">Українська</a> |
  <a href="README.bn.md">বাংলা</a> |
  <a href="README.gr.md">Ελληνικά</a> |
  <a href="README.vi.md">Tiếng Việt</a>
</p>

<p align="center">
  <img src="docs/preview.png" alt="VoiceToText preview" width="100%">
</p>

## ภาพรวม

VoiceToText เป็นวิดเจ็ตเนทีฟ Rust + Slint: กดคีย์ลัด พูด แล้วข้อความที่รู้จำจะถูกแทรกในแอปที่ใช้งานอยู่ ไม่ต้องใช้ Chromium, WebView2 หรือ Node.js

## คุณสมบัติ

- แผงโค้งมน 121 × 32 px อยู่บนสุด ลากได้ และซ่อนจากทาสก์บาร์
- คีย์ลัดทั่วไป (ค่าเริ่มต้น F8), Groq whisper-large-v3 และโมเดล Whisper ในเครื่อง
- ถอดเสียงและแปลด้วย openai/gpt-oss-120b พร้อม fallback อัตโนมัติไปยัง gpt-oss-20b
- คีย์ Groq สองคีย์หมุนอัตโนมัติและรองรับ 93 ภาษา
- ธีมสว่าง/มืดสำหรับแผง เมนู ตั้งค่า การแจ้งเตือน และถาดระบบ; บันทึกได้สูงสุด 5 นาที

## ดาวน์โหลด

[Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases/latest)

| Platform | Package |
| --- | --- |
| Windows 10/11 (x64) | `VoiceToText-windows-x64-setup.exe` |
| Linux (x64) | `VoiceToText-linux-x64.deb` / `VoiceToText-linux-x64.AppImage` |
| macOS Apple Silicon | `VoiceToText-macos-arm64.dmg` |
| macOS Intel | `VoiceToText-macos-x64.dmg` |

> ดาวน์โหลดแพ็กเกจสำหรับแพลตฟอร์มของคุณจาก Releases ตัวติดตั้งไม่ได้ลงนาม SmartScreen หรือ Gatekeeper อาจเตือน

## เริ่มต้นอย่างรวดเร็ว

1. ดาวน์โหลดและติดตั้งแพ็กเกจสำหรับระบบของคุณ
2. เปิด VoiceToText แผงจะปรากฏที่ตำแหน่งล่าสุด
3. กด F8 พูด แล้วกดอีกครั้ง ข้อความจะถูกแทรก
4. คลิกขวา → API Keys สำหรับ Groq หรือ Modelos สำหรับโมเดลในเครื่อง

## การใช้งาน

- ลากด้วยปุ่มซ้าย คลิกขวาเปิดเมนู Esc ปิดเมนู
- ไอคอนถาดระบบใช้แสดง ซ่อน หรือออก
- หน้าตั้งค่า API Keys และโมเดลจะถูกสร้างเมื่อต้องใช้และคืนหน่วยความจำเมื่อปิด
- หลังซ่อนแผง F8 ครั้งแรกจะแสดงแผง ส่วนครั้งถัดไปเริ่มบันทึก

## การตั้งค่าและข้อมูล

การตั้งค่าอยู่ที่ VoiceToTextWidget/settings.json ในโฟลเดอร์ผู้ใช้ และโมเดล Whisper อยู่ที่ Models การแทรกข้อความใช้คลิปบอร์ด + Ctrl/Cmd+V พร้อมพิมพ์สำรอง

## สร้างจากซอร์ส

```bash
cd app/src-tauri
cargo build --release
cargo test
```

ต้องใช้ Rust stable, CMake และ LLVM/libclang; Linux ต้องมี ALSA/X11/Wayland/libxdo และ macOS ต้องมี Xcode tools

[เอกสารภาษาอังกฤษฉบับเต็ม](README.md) · [Issues](https://github.com/sergio-dev1-py/openSpeakVoice/issues) · [Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases)

## สัญญาอนุญาต

MIT © 2026 sergio-dev1-py
