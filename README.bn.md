<p align="center">
  <a href="https://github.com/sergio-dev1-py/openSpeakVoice">
    <img src="app/src-tauri/icons/128x128.png" width="96" alt="VoiceToText logo">
  </a>
</p>

<h1 align="center">VoiceToText</h1>

<p align="center">
  <strong>বলুন। লিখুন। অনুবাদ করুন। যেকোনো জায়গায়।</strong><br>
  Windows, Linux ও macOS-এর জন্য ছোট ভাসমান ভয়েস-টু-টেক্সট পিল।
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
  <strong>বাংলা</strong> |
  <a href="README.gr.md">Ελληνικά</a> |
  <a href="README.vi.md">Tiếng Việt</a>
</p>

<p align="center">
  <img src="docs/preview.png" alt="VoiceToText preview" width="100%">
</p>

## সংক্ষিপ্ত বিবরণ

VoiceToText একটি নেটিভ Rust + Slint উইজেট: গ্লোবাল হটকি চাপুন, কথা বলুন, এবং চেনা লেখা সরাসরি সক্রিয় অ্যাপে ঢুকে যাবে। Chromium, WebView2 বা Node.js দরকার নেই।

## বৈশিষ্ট্য

- ১২১ × ৩২ পিক্সেল গোলাকার পিল, সর্বদা উপরে, ড্র্যাগ করা যায় এবং টাস্কবারে দেখা যায় না।
- গ্লোবাল হটকি (ডিফল্ট F8), Groq whisper-large-v3 এবং লোকাল Whisper মডেল।
- ট্রান্সক্রিপশন ও অনুবাদ: openai/gpt-oss-120b, ব্যর্থ হলে স্বয়ংক্রিয়ভাবে gpt-oss-20b।
- দুটি Groq API কী স্বয়ংক্রিয় রোটেশন এবং ৯৩টি ভাষা।
- পিল, মেনু, সেটিংস, টোস্ট ও ট্রের লাইট/ডার্ক থিম; ৫ মিনিট পর্যন্ত রেকর্ডিং।

## ডাউনলোড

[Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases/latest)

| Platform | Package |
| --- | --- |
| Windows 10/11 (x64) | `VoiceToText-windows-x64-setup.exe` |
| Linux (x64) | `VoiceToText-linux-x64.deb` / `VoiceToText-linux-x64.AppImage` |
| macOS Apple Silicon | `VoiceToText-macos-arm64.dmg` |
| macOS Intel | `VoiceToText-macos-x64.dmg` |

> Releases থেকে আপনার প্ল্যাটফর্মের প্যাকেজ ডাউনলোড করুন। ইনস্টলার স্বাক্ষরিত নয়; SmartScreen বা Gatekeeper সতর্ক করতে পারে।

## দ্রুত শুরু

1. আপনার সিস্টেমের প্যাকেজ ডাউনলোড ও ইনস্টল করুন।
2. VoiceToText চালু করুন; পিলটি শেষ সংরক্ষিত জায়গায় দেখাবে।
3. F8 চেপে কথা বলুন, আবার চাপুন; লেখা সক্রিয় অ্যাপে ঢুকে যাবে।
4. ডান ক্লিক → API Keys-এ Groq সেট করুন বা Modelos থেকে লোকাল মডেল নিন।

## ব্যবহার

- বাম ক্লিকে ড্র্যাগ করুন; ডান ক্লিকে মেনু; Esc বন্ধ করে।
- ট্রে আইকন দিয়ে দেখানো, লুকানো বা বন্ধ করা যায়।
- সেটিংস, API Keys ও মডেল উইন্ডো দরকারে তৈরি হয় ও বন্ধ হলে মুক্তি পায়।
- লুকানোর পর প্রথম F8 শুধু পিল দেখায়, দ্বিতীয়বার রেকর্ডিং শুরু হয়।

## সেটিংস ও ডেটা

সেটিংস ব্যবহারকারীর ফোল্ডারে VoiceToTextWidget/settings.json-এ, Whisper মডেল Models ফোল্ডারে থাকে। টেক্সট ইনসার্শন ক্লিপবোর্ড + Ctrl/Cmd+V, ব্যর্থ হলে কীবোর্ড টাইপিং।

## সোর্স থেকে বিল্ড

```bash
cd app/src-tauri
cargo build --release
cargo test
```

Rust stable, CMake ও LLVM/libclang দরকার; Linux-এ ALSA/X11/Wayland/libxdo, macOS-এ Xcode টুলস।

[সম্পূর্ণ ইংরেজি ডকুমেন্টেশন](README.md) · [Issues](https://github.com/sergio-dev1-py/openSpeakVoice/issues) · [Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases)

## লাইসেন্স

MIT © 2026 sergio-dev1-py
