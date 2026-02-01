@echo off
REM Windows x86_64 向け LiteRT C API DLL ビルドスクリプト
REM 前提: Bazel がインストール済み、LiteRT ソースが LITERT_DIR に存在すること

setlocal

set LITERT_DIR=%~dp0..\..\LiteRT
set OUTPUT_DIR=%~dp0..\Assets\Plugins\Windows\x86_64

if not exist "%LITERT_DIR%\WORKSPACE" (
    if not exist "%LITERT_DIR%\MODULE.bazel" (
        echo エラー: LiteRT ソースが見つかりません: %LITERT_DIR%
        echo LITERT_DIR 環境変数を設定するか、LiteRT リポジトリを配置してください
        exit /b 1
    )
)

echo === Windows x86_64 LiteRT C API DLL ビルド ===
echo LiteRT ソース: %LITERT_DIR%
echo 出力先: %OUTPUT_DIR%

pushd "%LITERT_DIR%"

REM configure を実行 (.litert_configure.bazelrc 生成)
if exist configure.bat (
    call configure.bat --workspace="%LITERT_DIR%"
) else if exist configure (
    python configure --workspace="%LITERT_DIR%"
)

echo.
echo --- Windows x86_64 DLL ビルド ---
bazel build //litert/c:litert_runtime_c_api_dll
if errorlevel 1 (
    echo エラー: ビルドに失敗しました
    popd
    exit /b 1
)

REM 成果物をコピー
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

REM Bazel 出力ディレクトリから DLL を探してコピー
for /r bazel-bin\litert\c %%f in (*.dll) do (
    echo コピー: %%f → %OUTPUT_DIR%
    copy /y "%%f" "%OUTPUT_DIR%\"
)

popd

echo.
echo === ビルド完了 ===
echo 成果物: %OUTPUT_DIR%
dir "%OUTPUT_DIR%\*.dll" 2>nul

endlocal
