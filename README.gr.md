<p align="center">
  <a href="https://github.com/sergio-dev1-py/openSpeakVoice">
    <img src="app/src-tauri/icons/128x128.png" width="96" alt="VoiceToText logo">
  </a>
</p>

<h1 align="center">VoiceToText</h1>

<p align="center">
  <strong>Μιλήστε. Μεταγράψτε. Μεταφράστε. Παντού.</strong><br>
  Ένα μικροσκοπικό αιωρούμενο χάπι φωνής-σε-κείμενο για Windows, Linux και macOS.
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
  <strong>Ελληνικά</strong> |
  <a href="README.vi.md">Tiếng Việt</a>
</p>

<p align="center">
  <img src="docs/preview.png" alt="VoiceToText preview" width="100%">
</p>

## Επισκόπηση

Το VoiceToText είναι ένα native widget Rust + Slint: πατήστε τη γενική συντόμευση, μιλήστε και το κείμενο εισάγεται απευθείας στην ενεργή εφαρμογή. Χωρίς Chromium, WebView2 ή Node.js.

## Χαρακτηριστικά

- Στρογγυλό χάπι 121 × 32 px, πάντα πάνω, σύρεται και κρύβεται από τη γραμμή εργασιών.
- Γενική συντόμευση (F8), Groq whisper-large-v3 και τοπικά μοντέλα Whisper.
- Μεταγραφή και μετάφραση με openai/gpt-oss-120b και αυτόματη εναλλαγή σε gpt-oss-20b.
- Δύο κλειδιά Groq με εναλλαγή και 93 γλώσσες αναγνώρισης.
- Φωτεινό/σκοτεινό θέμα σε χάπι, μενού, ρυθμίσεις, ειδοποιήσεις και δίσκο; εγγραφή έως 5 λεπτά.

## Λήψη

[Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases/latest)

| Platform | Package |
| --- | --- |
| Windows 10/11 (x64) | `VoiceToText-windows-x64-setup.exe` |
| Linux (x64) | `VoiceToText-linux-x64.deb` / `VoiceToText-linux-x64.AppImage` |
| macOS Apple Silicon | `VoiceToText-macos-arm64.dmg` |
| macOS Intel | `VoiceToText-macos-x64.dmg` |

> Κατεβάστε το πακέτο για την πλατφόρμα σας από τα Releases. Τα installers δεν είναι υπογεγραμμένα· SmartScreen ή Gatekeeper μπορεί να προειδοποιήσουν.

## Γρήγορη εκκίνηση

1. Κατεβάστε και εγκαταστήστε το πακέτο για το σύστημά σας.
2. Ανοίξτε το VoiceToText· το χάπι εμφανίζεται στην τελευταία θέση.
3. Πατήστε F8, μιλήστε και ξανά· το κείμενο εισάγεται.
4. Δεξί κλικ → API Keys για Groq ή Modelos για τοπικό μοντέλο.

## Χρήση

- Σύρετε με αριστερό κλικ· δεξί κλικ ανοίγει το μενού· Esc το κλείνει.
- Το εικονίδιο δίσκου εμφανίζει, κρύβει ή τερματίζει.
- Τα παράθυρα ρυθμίσεων, API Keys και μοντέλων δημιουργούνται κατά απαίτηση και ελευθερώνονται.
- Μετά την απόκρυψη, το πρώτο F8 δείχνει μόνο το χάπι· το δεύτερο ξεκινά εγγραφή.

## Ρυθμίσεις και δεδομένα

Οι ρυθμίσεις είναι στο VoiceToTextWidget/settings.json στον φάκελο χρήστη και τα μοντέλα Whisper στον φάκελο Models. Η εισαγωγή χρησιμοποιεί πρόχειρο + Ctrl/Cmd+V με εναλλακτική πληκτρολόγηση.

## Κατασκευή από τον κώδικα

```bash
cd app/src-tauri
cargo build --release
cargo test
```

Απαιτεί Rust stable, CMake και LLVM/libclang· στο Linux και ALSA/X11/Wayland/libxdo, στο macOS εργαλεία Xcode.

[Πλήρης αγγλική τεκμηρίωση](README.md) · [Issues](https://github.com/sergio-dev1-py/openSpeakVoice/issues) · [Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases)

## Άδεια

MIT © 2026 sergio-dev1-py
