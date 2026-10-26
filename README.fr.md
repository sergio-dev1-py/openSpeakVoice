<p align="center">
  <a href="https://github.com/sergio-dev1-py/openSpeakVoice">
    <img src="app/src-tauri/icons/128x128.png" width="96" alt="VoiceToText logo">
  </a>
</p>

<h1 align="center">VoiceToText</h1>

<p align="center">
  <strong>Parlez. Transcrivez. Traduisez. Partout.</strong><br>
  Une minuscule pastille flottante de dictée pour Windows, Linux et macOS.
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
  <strong>Français</strong> |
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

## Aperçu

VoiceToText est un widget natif Rust + Slint : appuyez sur le raccourci global, parlez, et le texte reconnu est inséré dans l'application active. Sans Chromium, WebView2 ni Node.js.

## Fonctionnalités

- Pastille arrondie de 121 × 32 px, toujours au-dessus, déplaçable et absente de la barre des tâches.
- Raccourci global (F8 par défaut), Groq whisper-large-v3 et modèles Whisper locaux.
- Transcription et traduction avec openai/gpt-oss-120b, repli automatique sur gpt-oss-20b.
- Deux clés API Groq avec rotation automatique et 93 langues de reconnaissance.
- Thèmes clair/sombre sur la pastille, le menu, les réglages, les toasts et la zone de notification ; limite de 5 minutes.

## Téléchargement

[Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases/latest)

| Platform | Package |
| --- | --- |
| Windows 10/11 (x64) | `VoiceToText-windows-x64-setup.exe` |
| Linux (x64) | `VoiceToText-linux-x64.deb` / `VoiceToText-linux-x64.AppImage` |
| macOS Apple Silicon | `VoiceToText-macos-arm64.dmg` |
| macOS Intel | `VoiceToText-macos-x64.dmg` |

> Téléchargez le paquet de votre plateforme depuis Releases. Les installateurs ne sont pas signés ; SmartScreen ou Gatekeeper peuvent avertir.

## Démarrage rapide

1. Téléchargez et installez le paquet correspondant à votre système.
2. Lancez VoiceToText ; la pastille apparaît à la dernière position enregistrée.
3. Appuyez sur F8, parlez, puis appuyez à nouveau ; le texte est inséré.
4. Clic droit → API Keys pour Groq, ou Modelos pour un modèle local.

## Utilisation

- Glisser avec le bouton gauche déplace la pastille ; clic droit ouvre le menu ; Échap le ferme.
- L'icône de la zone de notification permet d'afficher, masquer ou quitter.
- Réglages, API Keys et Modèles sont créés à la demande puis libérés.
- Après masquage, le premier F8 réaffiche la pastille ; le second démarre l'enregistrement.

## Réglages et données

Les réglages sont dans VoiceToTextWidget/settings.json dans votre dossier utilisateur ; les modèles Whisper dans le dossier Models. L'injection utilise le presse-papiers + Ctrl/Cmd+V avec repli clavier.

## Compiler depuis les sources

```bash
cd app/src-tauri
cargo build --release
cargo test
```

Nécessite Rust stable, CMake et LLVM/libclang ; sous Linux ALSA/X11/Wayland/libxdo, sous macOS les outils Xcode.

[Documentation complète en anglais](README.md) · [Issues](https://github.com/sergio-dev1-py/openSpeakVoice/issues) · [Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases)

## Licence

MIT © 2026 sergio-dev1-py
