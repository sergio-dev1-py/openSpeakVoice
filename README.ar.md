<p align="center">
  <a href="https://github.com/sergio-dev1-py/openSpeakVoice">
    <img src="app/src-tauri/icons/128x128.png" width="96" alt="VoiceToText logo">
  </a>
</p>

<h1 align="center">VoiceToText</h1>

<p align="center">
  <strong>تحدث. اكتب. ترجم. في أي مكان.</strong><br>
  حبوبة عائمة صغيرة لتحويل الصوت إلى نص على Windows وLinux وmacOS.
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
  <strong>العربية</strong> |
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

## نظرة عامة

VoiceToText أداة أصلية مبنية بـ Rust + Slint: اضغط الاختصار العام، تحدث، وسيُدرج النص المُتعرَّف عليه مباشرة في التطبيق النشط. بدون Chromium أو WebView2 أو Node.js.

## الميزات

- حبوبة مستديرة 121 × 32 بكسل، دائمًا في الأعلى، قابلة للسحب، ومخفية من شريط المهام.
- اختصار عام (F8 افتراضيًا)، ونموذج Groq whisper-large-v3، ونماذج Whisper المحلية.
- النسخ والترجمة باستخدام openai/gpt-oss-120b مع الرجوع تلقائيًا إلى gpt-oss-20b.
- مفتاحا Groq API مع تبديل تلقائي و93 لغة تعرف.
- سمة فاتحة/داكنة للحبوبة والقائمة والإعدادات والتنبيهات وصينية النظام؛ تسجيل حتى 5 دقائق.

## التحميل

[Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases/latest)

| Platform | Package |
| --- | --- |
| Windows 10/11 (x64) | `VoiceToText-windows-x64-setup.exe` |
| Linux (x64) | `VoiceToText-linux-x64.deb` / `VoiceToText-linux-x64.AppImage` |
| macOS Apple Silicon | `VoiceToText-macos-arm64.dmg` |
| macOS Intel | `VoiceToText-macos-x64.dmg` |

> نزّل حزمة النظام المناسب من Releases. ملفات التثبيت غير موقعة؛ قد يظهر تحذير من SmartScreen أو Gatekeeper.

## البدء السريع

1. نزّل حزمة نظامك وثبّتها.
2. شغّل VoiceToText وستظهر الحبوبة في آخر موضع محفوظ.
3. اضغط F8 وتحدث ثم اضغط مجددًا؛ سيُدرج النص في التطبيق النشط.
4. انقر بالزر الأيمن → API Keys لإعداد Groq، أو Modelos لتنزيل نموذج محلي.

## الاستخدام

- اسحب بالزر الأيسر لتحريك الحبوبة؛ الزر الأيمن يفتح القائمة؛ Esc يغلقها.
- أيقونة صينية النظام تتيح الإظهار أو الإخفاء أو الخروج.
- نوافذ الإعدادات ومفاتيح API والنماذج تُنشأ عند الحاجة وتُحرَّر عند الإغلاق.
- بعد الإخفاء، أول F8 يعيد إظهار الحبوبة فقط، والثاني يبدأ التسجيل.

## الإعدادات والبيانات

تُحفظ الإعدادات في VoiceToTextWidget/settings.json داخل مجلد المستخدم، ونماذج Whisper في مجلد Models. إدراج النص يستخدم الحافظة + Ctrl/Cmd+V مع بديل للكتابة.

## البناء من المصدر

```bash
cd app/src-tauri
cargo build --release
cargo test
```

يتطلب Rust stable وCMake وLLVM/libclang؛ على Linux أيضًا ALSA/X11/Wayland/libxdo، وعلى macOS أدوات Xcode.

[التوثيق الإنجليزي الكامل](README.md) · [Issues](https://github.com/sergio-dev1-py/openSpeakVoice/issues) · [Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases)


## الترخيص

MIT © 2026 sergio-dev1-py
