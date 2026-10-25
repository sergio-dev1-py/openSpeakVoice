@echo off
REM Lanza VoiceToText (Slint + Rust). Si Cargo esta disponible actualiza la
REM version release antes de abrirla; si no, usa el ejecutable ya compilado.
setlocal
cd /d "%~dp0"

set "APP=src-tauri\target\release\voicetotext.exe"

where cargo >nul 2>nul
if errorlevel 1 goto :sin_cargo

if defined LIBCLANG_PATH (
  if not exist "%LIBCLANG_PATH%\libclang.dll" set "LIBCLANG_PATH="
)
if not defined LIBCLANG_PATH (
  if exist "..\tools\clang+llvm-18.1.8-x86_64-pc-windows-msvc\bin\libclang.dll" (
    set "LIBCLANG_PATH=%~dp0..\tools\clang+llvm-18.1.8-x86_64-pc-windows-msvc\bin"
  )
)
if defined LIBCLANG_PATH (
  set "PATH=%LIBCLANG_PATH%;%PATH%"
)
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe" (
  set "PATH=%PATH%;C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin"
)

echo Compilando/actualizando la version release...
echo.
pushd src-tauri
cargo build --release
set "BUILD_RESULT=%ERRORLEVEL%"
popd

if not "%BUILD_RESULT%"=="0" (
  echo.
  echo ERROR: fallo la compilacion. Revisa los mensajes de arriba.
  pause
  exit /b %BUILD_RESULT%
)

if not exist "%APP%" (
  echo ERROR: la compilacion termino pero no se encontro %APP%.
  pause
  exit /b 1
)

start "" "%APP%"
exit /b 0

:sin_cargo
if exist "%APP%" (
  start "" "%APP%"
  exit /b 0
)
echo ERROR: no se encontro el ejecutable release ni Rust/Cargo en el PATH.
echo Instala Rust desde https://rustup.rs y vuelve a ejecutar este archivo.
pause
exit /b 1
