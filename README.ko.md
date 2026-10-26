<p align="center">
  <a href="https://github.com/sergio-dev1-py/openSpeakVoice">
    <img src="app/src-tauri/icons/128x128.png" width="96" alt="VoiceToText logo">
  </a>
</p>

<h1 align="center">VoiceToText</h1>

<p align="center">
  <strong>말하고, 기록하고, 번역하세요. 어디서든.</strong><br>
  Windows, Linux, macOS용 초소형 항상 위 음성-텍스트 필.
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
  <strong>한국어</strong> |
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

## 개요

VoiceToText는 네이티브 Rust + Slint 데스크톱 위젯입니다. 전역 단축키를 누르고 말하면 인식된 텍스트가 현재 앱에 바로 입력됩니다. Chromium, WebView2, Node.js가 필요 없습니다.

## 기능

- 121 × 32 px 둥근 필, 항상 위, 드래그 가능, 작업 표시줄에 표시되지 않음.
- 전역 단축키(기본 F8), Groq whisper-large-v3 및 로컬 Whisper 모델 지원.
- 전사 및 번역: openai/gpt-oss-120b, 실패 시 gpt-oss-20b로 자동 전환.
- 두 개의 Groq API 키 자동 순환, 93개 인식 언어.
- 필, 메뉴, 설정, 토스트, 트레이의 라이트/다크 테마; 최대 5분 녹음.

## 다운로드

[Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases/latest)

| Platform | Package |
| --- | --- |
| Windows 10/11 (x64) | `VoiceToText-windows-x64-setup.exe` |
| Linux (x64) | `VoiceToText-linux-x64.deb` / `VoiceToText-linux-x64.AppImage` |
| macOS Apple Silicon | `VoiceToText-macos-arm64.dmg` |
| macOS Intel | `VoiceToText-macos-x64.dmg` |

> Releases에서 설치 파일을 받으세요. 설치 파일은 서명되지 않아 Windows SmartScreen 또는 macOS Gatekeeper 경고가 나올 수 있습니다.

## 빠른 시작

1. 사용 중인 OS용 패키지를 다운로드해 설치합니다.
2. VoiceToText를 실행하면 마지막 위치에 필이 나타납니다.
3. F8을 눌러 말하고 다시 눌러 종료하면 텍스트가 현재 앱에 입력됩니다.
4. 오른쪽 클릭 → API Keys에서 Groq 키를 설정하거나 Modelos에서 로컬 모델을 받으세요.

## 사용법

- 왼쪽 드래그로 필 이동, 오른쪽 클릭으로 메뉴, Esc로 닫기.
- 트레이 아이콘에서 표시/숨기기/종료.
- 설정, API Keys, 모델 창은 필요할 때 생성되고 닫으면 해제됩니다.
- 숨긴 뒤 첫 F8은 필만 표시하고, 두 번째 F8부터 녹음합니다.

## 설정 및 데이터

설정은 사용자 폴더의 VoiceToTextWidget/settings.json에 저장되며 Whisper 모델은 같은 폴더의 Models에 있습니다. 텍스트 주입은 클립보드 + Ctrl/Cmd+V를 사용하고 실패 시 키보드 입력으로 대체합니다.

## 소스에서 빌드

```bash
cd app/src-tauri
cargo build --release
cargo test
```

Rust stable, CMake, LLVM/libclang이 필요합니다. Linux는 ALSA/X11/Wayland/libxdo, macOS는 Xcode 도구가 추가로 필요합니다.

[전체 영어 문서](README.md) · [Issues](https://github.com/sergio-dev1-py/openSpeakVoice/issues) · [Releases](https://github.com/sergio-dev1-py/openSpeakVoice/releases)

## 라이선스

MIT © 2026 sergio-dev1-py
