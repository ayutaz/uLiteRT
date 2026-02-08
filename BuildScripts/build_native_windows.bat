@echo off

REM Copyright 2025 ayutaz
REM Licensed under the Apache License, Version 2.0
REM See LICENSE file or http://www.apache.org/licenses/LICENSE-2.0

REM Docker コンテナ内で実行される Windows DLL ビルドスクリプト
REM LiteRT C API の共有ライブラリ (DLL) をビルドする

setlocal enabledelayedexpansion

set OUTPUT_DIR=%OUTPUT_DIR%
if "%OUTPUT_DIR%"=="" set OUTPUT_DIR=C:\output

echo === LiteRT C API Windows DLL ビルド ===
echo 出力先: %OUTPUT_DIR%

cd /d C:\litert_build

REM msvc_compat.h を作成（空ファイルで十分な場合が多い）
if not exist "C:\BuildTools\LiteRT-LM\msvc_compat.h" (
    echo // MSVC compatibility header for LiteRT > "C:\BuildTools\LiteRT-LM\msvc_compat.h"
    echo #pragma once >> "C:\BuildTools\LiteRT-LM\msvc_compat.h"
)

REM configure を実行
echo.
echo --- configure 実行 ---
if exist configure.bat (
    call configure.bat --workspace=C:\litert_build
) else if exist configure (
    "C:\Program Files\Python311\python.exe" configure --workspace=C:\litert_build
)

REM DLL ビルド
echo.
echo --- Windows x86_64 DLL ビルド ---
bazel build --config=windows //litert/c:litert_runtime_c_api_dll
if errorlevel 1 (
    echo エラー: ビルドに失敗しました
    exit /b 1
)

REM 成果物をコピー
if not exist "%OUTPUT_DIR%\Windows\x86_64" mkdir "%OUTPUT_DIR%\Windows\x86_64"

for /r bazel-bin\litert\c %%f in (*.dll) do (
    echo コピー: %%f → %OUTPUT_DIR%\Windows\x86_64\
    copy /y "%%f" "%OUTPUT_DIR%\Windows\x86_64\"
)

echo.
echo === ビルド完了 ===
echo 成果物:
dir "%OUTPUT_DIR%\Windows\x86_64\*.dll" 2>nul

endlocal
