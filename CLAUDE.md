# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## プロジェクト概要

uLiteRT は、Google の LiteRT（旧 TensorFlow Lite）の新 CompiledModel C API を Unity から P/Invoke で利用するための汎用ライブラリ。
Android / iOS / Windows をターゲットとし、CPU・GPU・NPU アクセラレーションに対応する。

- **LiteRT リポジトリ:** https://github.com/google-ai-edge/LiteRT
- **設計ドキュメント:** `../LiteRT/docs/unity-binding-guide.md`

## 開発環境

- **Unity バージョン:** 6000.3.2f1 (Unity 6 LTS)
- **ターゲットフレームワーク:** .NET 4.7.1
- **IDE:** Rider または Visual Studio
- **テストフレームワーク:** Unity Test Framework 1.6.0

## ビルド・テスト

```bash
# Unity エディタからビルド（CLI ビルドスクリプトは未整備）
# テストは Unity Test Runner (Window > General > Test Runner) から実行
```

### ネイティブライブラリビルド

LiteRT ソースは `../LiteRT/`（uLiteRT と同階層）に配置されている前提。
現在 LiteRT **v2.1.2** タグでビルド確認済み（main ブランチには `fp16.h` 再定義バグあり）。

```bash
# Android arm64 (Docker 経由、Linux/macOS/Windows いずれも可)
# 前提: Docker が起動していること
bash BuildScripts/build_all.sh
# → Assets/Plugins/Android/arm64-v8a/libLiteRt.so が生成される

# Windows x86_64 (ローカル Bazel、未検証)
# 前提: Bazel がインストール済みであること
BuildScripts\build_native.bat
# → Assets/Plugins/Windows/x86_64/libLiteRt.dll が生成される
```

#### ビルドスクリプト構成

| ファイル | 説明 |
|---|---|
| `BuildScripts/Dockerfile` | Android ビルド用 Docker 環境 (Ubuntu 24.04, Bazel 7.4.1, Android NDK r28b) |
| `BuildScripts/build_native.sh` | Docker 内で実行される Android arm64 ビルドスクリプト |
| `BuildScripts/build_native.bat` | Windows ローカルで実行する DLL ビルドスクリプト |
| `BuildScripts/build_all.sh` | Docker ビルド起動 + Assets/Plugins/ への成果物コピー |

#### Windows (Git Bash) での注意事項

- `MSYS_NO_PATHCONV=1` が必要（`build_all.sh` 内で自動設定済み）
- LiteRT ソースの CRLF 改行は Docker エントリーポイントで `git checkout` により自動変換される

#### Bazel ビルドターゲット (参考)

```bash
# Android (ARM64)
bazel build --config=android_arm64 //litert/c:litert_runtime_c_api_so

# Windows (x86_64)
bazel build //litert/c:litert_runtime_c_api_dll

# macOS (Apple Silicon) — 将来対応
bazel build --config=macos_arm64 //litert/c:litert_runtime_c_api_dylib
```

## アーキテクチャ

### API方針

P/Invoke で LiteRT 2.x CompiledModel C API を直接呼び出す。プラットフォームごとにネイティブライブラリ (.dll/.so/.dylib) を差し替えるだけで、C# コードは共通。

### ネイティブライブラリ配置

```
Assets/Plugins/
  Android/arm64-v8a/libLiteRt.so   ← ビルド確認済み (v2.1.2)
  Windows/x86_64/libLiteRt.dll     ← 未検証
  macOS/libLiteRt.dylib            ← 将来対応
  iOS/libLiteRt.a (or .framework)  ← 将来対応
```

バイナリは `.gitignore` で除外されるため、各環境でビルドスクリプトを実行して配置する。

### C# クラス構成

```
Assets/LiteRT/Runtime/
  Native.cs                  — 全 P/Invoke 宣言 (internal static class)     [実装済み]
  Enums.cs                   — LiteRtStatus, LiteRtHwAccelerators 等        [実装済み]
  Structs.cs                 — LiteRtLayout, LiteRtRankedTensorType 等      [実装済み]
  LiteRtException.cs         — ステータス → 例外変換                        [実装済み]
  LiteRtEnvironment.cs       — IDisposable ラッパー                         [実装済み]
  LiteRtModel.cs             — モデル読み込み + シグネチャ情報              [実装済み]
  LiteRtOptions.cs           — コンパイルオプションビルダー                 [実装済み]
  LiteRtCompiledModel.cs     — 推論実行の高レベル API                       [実装済み]
  LiteRtTensorBuffer.cs      — バッファ管理 (Lock/Unlock/Dispose)           [実装済み]
  GpuOptions.cs              — GPU 固有オプション                           [実装済み]
  CpuOptions.cs              — CPU 固有オプション                           [実装済み]
  RuntimeOptions.cs           — ランタイム固有オプション                     [実装済み]
  LiteRtTensorInfo.cs         — テンソルメタデータ（読み取り専用）           [実装済み]
  AssemblyInfo.cs             — InternalsVisibleTo 設定                      [実装済み]
```

### サンプル構成

```
Assets/LiteRT/Samples/
  Common/
    SampleBase.cs              — 全サンプル共通基盤 (GPU/CPU 切替、モデルロード)
    ImageHelper.cs             — 画像前処理ユーティリティ
    LabelLoader.cs             — ラベル・マッピング読み込み
    AssemblyInfo.cs            — InternalsVisibleTo 設定
  ImageClassification/         — MobileNet V2 画像分類
  ObjectDetection/             — SSD MobileNet V1 物体検出
  ImageSegmentation/           — DeepLab V3 セグメンテーション
  PoseEstimation/              — PoseNet 姿勢推定
  StyleTransfer/               — Magenta スタイル変換
  SoundClassification/         — YAMNet 音声分類
  TextToSpeech/                — FastSpeech2 + MB-MelGAN TTS
```

### テスト構成 (122テスト)

```
Assets/LiteRT/Tests/Runtime/
  StructLayoutTests.cs              — 構造体サイズ・オフセット検証 (8テスト)
  EnumTests.cs                      — 列挙値の C ヘッダーとの一致検証 (8テスト)
  NewEnumTests.cs                   — 拡張 enum の値検証 (6テスト)
  StructOperationTests.cs           — 構造体操作テスト (18テスト)
  ExceptionTests.cs                 — 例外変換テスト (6テスト)
  InputValidationTests.cs           — 入力バリデーションテスト (6テスト)
  CalculatePackedBufferSizeTests.cs — バッファサイズ計算テスト (21テスト)
  DisposeSafetyTests.cs             — Dispose 安全性テスト (16テスト)

Assets/LiteRT/Tests/Samples/
  ImageHelperTests.cs               — 画像前処理テスト (11テスト)
  LabelLoaderTests.cs               — ラベル読み込みテスト (11テスト)
  EstimateDynamicBufferSizeTests.cs — 動的バッファサイズ推定テスト (11テスト)
```

### API呼び出しフロー

```
初期化: Environment → Model → Options → CompiledModel → BufferRequirements → TensorBuffer
推論:   Lock(input,Write) → memcpy → Unlock → Run → Lock(output,Read) → read → Unlock
破棄:   TensorBuffer → CompiledModel → Model → Environment (逆順)
```

### 重要な技術的注意事項

- `size_t` は C# で `UIntPtr` にマーシャリングする
- C の `bool` は 1バイト → `[MarshalAs(UnmanagedType.I1)]` 必須
- HostMemory バッファは **64バイトアライメント** 必須（`LiteRtCreateManagedTensorBuffer` 推奨）
- `LiteRtCreateModelFromBuffer` は Model 寿命中バッファを保持する必要がある（GCHandle.Alloc で pin）
- GPU バッファ連携は `GL.IssuePluginEvent` でレンダリングスレッドと同期が必要
- `LiteRtTensorBufferRequirements` は CompiledModel が所有（Destroy 不要）
- Windows の `EntryPointNotFoundException` は `windows_exported_symbols.def` にシンボル追加が必要な場合がある

## Windows GPU 対応調査 (2026-02-10)

### 現状: GPU コンパイルは失敗、CPU フォールバックで動作

テスト環境: NVIDIA GeForce RTX 4070 Ti SUPER, Direct3D 12 [level 12.2], Windows

### 調査で判明した2段階の問題

#### 問題1: GPU 環境が構築されない (解決済み)

- **原因**: `LiteRtCreateEnvironment(0, IntPtr.Zero, ...)` だけでは GPU 環境が作成されない。C API 側 (`litert_environment.cc:55-66`) で `has_gpu_options == true` の場合のみ GPU 環境を構築する仕組み
- **対策**: `LiteRtGpuEnvironmentCreate` を明示的に呼び出すよう修正。`LiteRtEnvironment.CreateGpuEnvironment()` メソッドを追加し、`SampleBase.LoadModel()` で GPU 選択時に呼び出す
- **結果**: `HasGpuEnvironment` が `False` → `True` に改善。OpenCL デバイスの検出・コンテキスト作成に成功

#### 問題2: GPU モデルコンパイルが ErrorCompilation で失敗 (未解決)

GPU 環境は構築されるが、`LiteRtCreateCompiledModel` で `ErrorCompilation` が発生する。

**推定原因の候補:**

1. **OpenCL オペレータ未サポート**: LiteRT の OpenCL GPU デリゲートが当該モデル（mb_melgan.tflite）の一部オペレータに非対応の可能性。TTS モデルは複雑な演算グラフを含むため、全オペレータが GPU デリゲートでサポートされていない場合がある
2. **Windows OpenCL の制限**: LiteRT の GPU デリゲートは主に Android (OpenCL/OpenGL) と iOS (Metal) 向けに最適化されており、Windows の OpenCL サポートは限定的
3. **Bazel ビルドフラグ**: Windows DLL ビルド時に `LITERT_HAS_OPENCL_SUPPORT` が有効であるか未確認。ビルドフラグが不足している場合、GPU コンパイルパスが不完全な可能性

### Windows GPU バックエンド対応状況

| バックエンド | 対応状況 | 備考 |
|---|---|---|
| OpenCL | 環境構築可、コンパイル失敗 | NVIDIA ドライバの OpenCL.dll を検出・ロードに成功するが、モデルコンパイルで ErrorCompilation |
| Vulkan | 実験的 | バッファ作成関数が未エクスポート（Getter のみ） |
| WebGPU | 別 DLL 必要 | `libLiteRtWebGpuAccelerator.dll` が別途必要 |
| Metal | 非対応 | Apple 専用 |
| OpenGL | 非対応 | Linux/Android 専用 |

### GPU 初期化フロー (現在の実装)

```
Environment 作成 → CreateGpuEnvironment() → [成功] → Model → Options(GPU) → CompiledModel
                                            → [失敗] → CPU フォールバック

CompiledModel 作成 → [ErrorCompilation] → CPU フォールバック（現在ここで失敗）
```

### 実際のログ出力

```
GPU 環境初期化完了
GPU コンパイル失敗。CPU にフォールバックします。
  エラー: LiteRT error: ErrorCompilation (Status: ErrorCompilation)
  GPU デバイス: NVIDIA GeForce RTX 4070 Ti SUPER
  GPU API: Direct3D12 (Direct3D 12 [level 12.2])
  Compute Shader: True
  Graphics Memory: 16063 MB
  LiteRT GPU 環境: True
モデル読み込み完了: mb_melgan.tflite
```

### 今後の調査方針

1. **エラーメッセージの詳細取得**: `LiteRtCompiledModelGetErrorMessages` で GPU コンパイル失敗の具体的な理由（未サポートオペレータ名等）を取得する
2. **シンプルなモデルでの検証**: MobileNet 等の標準的なモデルで GPU コンパイルが成功するか確認し、モデル固有の問題かプラットフォームの問題かを切り分ける
3. **Bazel ビルドフラグ確認**: Windows DLL ビルド時の `LITERT_HAS_OPENCL_SUPPORT` フラグの有無を確認
4. **Android 実機での GPU テスト**: Android (OpenCL/OpenGL) は LiteRT の主要ターゲットであり、GPU 推論が動作する可能性が高い

## 言語

- コードコメント・ドキュメント・コミットメッセージは日本語で記述する
