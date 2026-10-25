# VoiceToText Widget

Píldora flotante de voz-a-texto para escritorio, escrita en **Rust + Slint**. Sustituye por completo la versión Tauri/WebView2: **no usa Chromium, WebView2, Node.js ni un frontend web**.

Pulsa el atajo global (por defecto **F8**), habla, vuelve a pulsar y el texto se transcribe e inserta automáticamente donde tengas el cursor.

![píldora](app/src-tauri/icons/128x128.png)

## Características

- **Píldora 121×32 px** sin marco, transparente, siempre visible y arrastrable.
- **Botón ▶ / ■** integrado para iniciar o detener la grabación (además del atajo global).
- **Clic derecho en cualquier parte de la píldora** para abrir el menú contextual tematizado.
- **Temas claro/oscuro** aplicados a píldora, menú, Ajustes, API Keys, Modelos, toast y bandeja del sistema.
  - Tema claro: píldora blanca con borde negro y rayo negro.
  - Tema oscuro: píldora oscura con rayo blanco.
  - El color/opacidad del borde y el modo multicolor se aplican a la onda y al borde activo.
- **Onda de audio real** con historial de barras: se mueve al hablar y queda estática en reposo. No se actualiza ni redibuja cuando no hay grabación.
- **Ventanas bajo demanda**: Ajustes/API Keys/Modelos, menú y toast se crean al abrirlos y se liberan al cerrar.
- **Límite de grabación de 5 minutos**: al alcanzarlo se detiene y transcribe automáticamente.
- **Arrastre nativo** (Windows) y persistencia automática de la posición.
- **Dos proveedores de reconocimiento**:
  - **Groq (nube)** — `whisper-large-v3` con dos API keys y rotación automática (19 req/min por key).
  - **Whisper (local)** — modelos ggml tiny/base/small/medium/large-v3 descargados y ejecutados sin conexión.
- **Modos transcripción y traducción**:
  - Traducción con `openai/gpt-oss-120b`.
  - Fallback automático a `openai/gpt-oss-20b` si el modelo principal no está disponible.
- **93 idiomas** de reconocimiento (selector con nombres nativos y código).
- **Atajo global configurable** (F1–F24, letras, dígitos, Space, etc. + Ctrl/Alt/Shift/Win).
- **Inyección de texto** por portapapeles + Ctrl+V (Cmd+V en macOS), con fallback de tipeo.
- **Bandeja del sistema** con icono adaptativo al tema: clic izquierdo muestra la píldora, clic derecho abre Mostrar/Ocultar/Salir.
- **settings.json compatible** con la versión C#/Tauri: reutiliza `%LOCALAPPDATA%\VoiceToTextWidget` y los modelos ya descargados.

## Motor de renderizado

La app fuerza el **renderizador software de Slint** (`SLINT_BACKEND=winit-software`).

Motivo: el equipo de destino usa una GPU Intel HD 3000 cuyo driver OpenGL no soporta crear el contexto que necesita FemtoVG. El renderizador software:

- No necesita contexto OpenGL/Vulkan/Direct3D.
- Mantiene la transparencia por píxel de la píldora.
- Usa un único proceso y muy poca memoria.
- Para una ventana de 121×32 px y ventanas de configuración ocasionales, el coste de CPU es mínimo.

Si quieres probar otro backend, define `SLINT_BACKEND` antes de arrancar (por ejemplo `winit-software`).

## Consumo medido (release, Windows)

- **1 solo proceso** (sin procesos auxiliares de WebView2/Chromium).
- **~16–20 MB** de memoria de trabajo en reposo; ~22 MB con Ajustes abierta.
- **~0,1 % de un núcleo** en reposo (medido durante 15 s), sin animación de onda.
- Ejecutable release de **~15 MB**.
- Referencia de la versión Tauri corregida: ~95 MB privados y 6 procesos en reposo.

La onda solo actualiza la UI a 30 fps mientras se está grabando; en reposo el temporizador baja a 10 Hz y no modifica la onda.

## Requisitos

| Plataforma | Requisitos |
|---|---|
| Windows 10/11 | Rust stable (MSVC), Visual Studio Build Tools (C++), CMake y LLVM/libclang. Windows probado. |
| Linux | Rust stable, ALSA, X11/Wayland dev, libxdo, CMake y libclang. Compilación no probada en esta máquina. |
| macOS 12+ | Rust stable, Xcode Command Line Tools, CMake y libclang. Permisos de Micrófono/Accesibilidad. Compilación no probada en esta máquina. |

No hace falta Node.js, npm, WebView2 ni WebKitGTK.

### Variables de entorno en Windows

`whisper-rs` usa `bindgen`, que necesita `libclang`:

```bat
set "LIBCLANG_PATH=C:\ruta\a\llvm\bin"
set "PATH=%PATH%;C:\ruta\a\cmake\bin"
```

En este repositorio el toolchain local está en:

```
tools\clang+llvm-18.1.8-x86_64-pc-windows-msvc\bin
```

## Compilación

```bat
cd app\src-tauri
cargo build --release
```

El ejecutable queda en:

```
app\src-tauri\target\release\voicetotext.exe
```

> **No uses `cargo rustc --release -- -C lto=off`.** El perfil release compila todas las dependencias con `lto = "fat"`; mezclar un binario sin LTO produce errores `LNK2001`/`LNK1136`. Para desarrollo usa `cargo build` (debug) o `cargo run`; para la release, siempre `cargo build --release`.

También puedes ejecutar directamente `app\iniciar-app.bat`: configura `LIBCLANG_PATH`/CMake, actualiza la versión release si hay cambios (si no hay cambios Cargo termina en segundos) y la abre. Si Rust no está instalado pero el `.exe` ya existe, lo abre directamente.

### Tests

```bat
cd app\src-tauri
cargo test
```

## Uso

- **F8** (o el atajo configurado): iniciar/detener grabación. Si la píldora está oculta, el primer F8 la muestra.
- **Clic izquierdo + arrastrar** sobre la píldora: moverla. La posición se guarda.
- **Clic derecho** sobre la píldora: menú de Ajustes, API Keys, proveedor, modo, idioma destino, ocultar y salir.
- **Bandeja del sistema**: clic izquierdo muestra la píldora; clic derecho abre el menú nativo.
- **Ajustes**: idioma de reconocimiento, modo, idioma destino, atajo, tema, color/opacidad del borde y multicolor. El campo del atajo acepta F1–F24, letras, dígitos, `Space`, `Enter`, `PageUp`, etc., combinados con Ctrl/Alt/Shift/Win.
- **API Keys**: dos claves de Groq con contador de uso por minuto.
- **Modelos**: descargar/cancelar/borrar modelos locales y activar/desactivar aceleración GPU (el ajuste se guarda; whisper.cpp se ejecuta en CPU salvo que se compile con soporte GPU).

## Estructura

```
app/
├─ iniciar-app.bat
└─ src-tauri/
   ├─ Cargo.toml
   ├─ build.rs              # compila ui/app.slint
   ├─ ui/app.slint          # píldora, menú, gestión, toast, bandeja
   ├─ icons/                # rayo monocromático e iconos de la app
   └─ src/
      ├─ lib.rs             # wiring Slint ↔ core, ventanas, atajos, bandeja
      ├─ core.rs            # estado, grabación, transcripción, traducción
      ├─ audio.rs           # captura cpal + resample a 16 kHz
      ├─ whisper.rs         # modelos ggml + whisper.cpp
      ├─ groq.rs            # Groq API (STT + traducción)
      ├─ inject.rs          # portapapeles/Ctrl+V y fallback de tipeo
      ├─ hotkey.rs          # nombres de teclas ⇄ W3C
      ├─ languages.rs       # tablas de idiomas
      ├─ settings.rs        # settings.json compatible con la versión C#
      └─ logger.rs          # app.log
```

## Datos y configuración

- Ajustes: `%LOCALAPPDATA%\VoiceToTextWidget\settings.json`
- Modelos: `%LOCALAPPDATA%\VoiceToTextWidget\Models\ggml-*.bin`
- Log: `%LOCALAPPDATA%\VoiceToTextWidget\app.log`

## Limitaciones conocidas

- El arrastre nativo y la colocación precisa de ventanas están implementados para **Windows**. En Linux/macOS no se ha probado la compilación en esta máquina; además, la píldora no se puede arrastrar con el ratón y las ventanas usan la posición por defecto del sistema.
- El CI publica **binarios sin instalador** con nombres por plataforma (`VoiceToText-windows-x64.exe`, `VoiceToText-linux-x64`, `VoiceToText-macos-arm64`, `VoiceToText-macos-x64`). No se generan NSIS/MSI/DMG/AppImage.
- La aceleración GPU de Whisper depende de cómo se compile `whisper-rs`; el checkbox solo guarda la preferencia.
- En macOS hay que conceder permisos de Micrófono y Accesibilidad.

## Historial

- **v2.0.0** — Migración completa a **Slint + Rust**. Sin Tauri, WebView2 ni frontend TypeScript. Píldora/menú/gestión/toast/bandeja nativos de Slint, renderizador software y un solo proceso.
- **v1.4.0** — Versión original C#/WinForms (se conserva en `VoiceToTextWidget/` como referencia).
