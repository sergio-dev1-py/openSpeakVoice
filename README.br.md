<p align="center">
  <a href="https://github.com/sergio-dev1-py/openSpeakVoice">
    <img src="app/src-tauri/icons/128x128.png" width="96" alt="VoiceToText logo">
  </a>
</p>

<h1 align="center">VoiceToText</h1>

<p align="center">
  <strong>Fale. Transcreva. Traduza. Em qualquer lugar.</strong><br>
  Uma pequena pílula flutuante de voz para texto para Windows, Linux e macOS.
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
  <strong>Português (Brasil)</strong> |
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

## Visão geral

VoiceToText é um widget nativo em Rust + Slint: pressione o atalho global, fale e o texto reconhecido é inserido diretamente no aplicativo ativo. Sem Chromium, WebView2 ou Node.js.

## Recursos

- Pílula arredondada de 121 × 32 px, sempre no topo, arrastável e oculta da barra de tarefas.
- Atalho global (F8 por padrão), Groq whisper-large-v3 e modelos Whisper locais.
- Transcrição e tradução com openai/gpt-oss-120b e fallback automático para gpt-oss-20b.
- Duas chaves Groq com rotação automática e 93 idiomas de reconhecimento.
- Temas claro/escuro na pílula, menu, configurações, toasts e bandeja; gravação de até 5 minutos.

## Download

[Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases/latest)

| Platform | Package |
| --- | --- |
| Windows 10/11 (x64) | `VoiceToText-windows-x64-setup.exe` |
| Linux (x64) | `VoiceToText-linux-x64.deb` / `VoiceToText-linux-x64.AppImage` |
| macOS Apple Silicon | `VoiceToText-macos-arm64.dmg` |
| macOS Intel | `VoiceToText-macos-x64.dmg` |

> Baixe o pacote da sua plataforma em Releases. Os instaladores não são assinados; SmartScreen ou Gatekeeper podem avisar.

## Início rápido

1. Baixe e instale o pacote do seu sistema.
2. Abra o VoiceToText; a pílula aparece na última posição salva.
3. Pressione F8, fale e pressione novamente; o texto é inserido.
4. Clique direito → API Keys para Groq ou Modelos para um modelo local.

## Uso

- Arraste com o botão esquerdo; clique direito abre o menu; Esc fecha.
- O ícone da bandeja mostra, oculta ou encerra o app.
- Configurações, API Keys e Modelos são criados sob demanda e liberados ao fechar.
- Após ocultar, o primeiro F8 apenas mostra a pílula; o segundo inicia a gravação.

## Configurações e dados

As configurações ficam em VoiceToTextWidget/settings.json na pasta do usuário; os modelos Whisper em Models. A injeção usa área de transferência + Ctrl/Cmd+V com fallback de teclado.

## Compilar do código-fonte

```bash
cd app/src-tauri
cargo build --release
cargo test
```

Requer Rust stable, CMake e LLVM/libclang; no Linux também ALSA/X11/Wayland/libxdo, no macOS ferramentas Xcode.

[Documentação completa em inglês](README.md) · [Issues](https://github.com/sergio-dev1-py/openSpeakVoice/issues) · [Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases)

## Licença

MIT © 2026 sergio-dev1-py
