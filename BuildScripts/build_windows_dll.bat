@echo off

REM Copyright 2025 ayutaz
REM Licensed under the Apache License, Version 2.0
REM See LICENSE file or http://www.apache.org/licenses/LICENSE-2.0

REM Windows DLL ビルドの Docker オーケストレーションスクリプト
REM 前提: Docker Desktop が Windows コンテナモードで起動していること
REM
REM 使い方:
REM   BuildScripts\build_windows_dll.bat

setlocal

set SCRIPT_DIR=%~dp0
set PROJECT_DIR=%SCRIPT_DIR%..
set LITERT_DIR=%SCRIPT_DIR%..\..\LiteRT
set PLUGINS_DIR=%PROJECT_DIR%\Assets\Plugins

set IMAGE_NAME=ulitert_build_windows
set CONTAINER_NAME=ulitert_build_windows_container

echo === uLiteRT Windows DLL ビルド (Docker) ===
echo LiteRT ソース: %LITERT_DIR%
echo 出力先: %PLUGINS_DIR%
echo.

REM LiteRT ソースの存在確認
if not exist "%LITERT_DIR%\WORKSPACE" (
    if not exist "%LITERT_DIR%\MODULE.bazel" (
        echo エラー: LiteRT ソースが見つかりません: %LITERT_DIR%
        echo LiteRT リポジトリを %LITERT_DIR% に配置してください
        exit /b 1
    )
)

REM Docker イメージビルド
echo --- Docker イメージビルド ---
docker build -t %IMAGE_NAME% -f "%SCRIPT_DIR%Dockerfile.windows" "%PROJECT_DIR%"
if errorlevel 1 (
    echo エラー: Docker イメージのビルドに失敗しました
    exit /b 1
)

REM 既存コンテナを削除
docker rm -f %CONTAINER_NAME% >nul 2>&1

REM 出力ディレクトリ作成
if not exist "%PLUGINS_DIR%\Windows\x86_64" mkdir "%PLUGINS_DIR%\Windows\x86_64"

REM Docker ビルド実行
echo.
echo --- Docker ビルド実行 ---
docker run --name %CONTAINER_NAME% ^
    -e OUTPUT_DIR=C:\output ^
    -v "%LITERT_DIR%:C:\litert_build" ^
    -v "%PLUGINS_DIR%:C:\output" ^
    %IMAGE_NAME%

if errorlevel 1 (
    echo エラー: ビルドに失敗しました
    exit /b 1
)

REM コンテナ削除
docker rm -f %CONTAINER_NAME% >nul 2>&1

echo.
echo === 完了 ===
echo 成果物:
dir "%PLUGINS_DIR%\Windows\x86_64\*.dll" 2>nul

endlocal
