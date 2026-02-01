# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## プロジェクト概要

uLiteRT は、Google の LiteRT（旧 TensorFlow Lite）の新 CompiledModel C API を Unity から P/Invoke で利用するための汎用ライブラリ。
Android / iOS / Windows をターゲットとし、CPU・GPU・NPU アクセラレーションに対応する。

- **LiteRT リポジトリ:** https://github.com/google-ai-edge/LiteRT
- **設計ドキュメント:** `C:\Users\yuta\Desktop\Private\LiteRT\docs\unity-binding-guide.md`

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

### ネイティブライブラリビルド (Bazel)

```bash
# Windows (x86_64)
bazel build --config=windows //litert/c:litert_runtime_c_api_dll

# Android (ARM64)
bazel build --config=android_arm64 //litert/c:litert_runtime_c_api_so

# macOS (Apple Silicon)
bazel build --config=macos_arm64 //litert/c:litert_runtime_c_api_dylib
```

## アーキテクチャ

### API方針

P/Invoke で LiteRT 2.x CompiledModel C API を直接呼び出す。プラットフォームごとにネイティブライブラリ (.dll/.so/.dylib) を差し替えるだけで、C# コードは共通。

### ネイティブライブラリ配置

```
Assets/Plugins/
  Windows/x86_64/libLiteRt.dll
  Android/libs/arm64-v8a/libLiteRt.so
  macOS/libLiteRt.dylib
  iOS/libLiteRt.a (or .framework)
```

### C# クラス構成 (計画)

```
Assets/LiteRT/Runtime/
  Native.cs                  — 全 P/Invoke 宣言 (static class)
  Enums.cs                   — LiteRtStatus, LiteRtHwAccelerators 等
  Structs.cs                 — LiteRtLayout, LiteRtRankedTensorType 等
  LiteRtException.cs         — ステータス → 例外変換
  LiteRtEnvironment.cs       — IDisposable ラッパー
  LiteRtModel.cs             — モデル読み込み + シグネチャ情報
  LiteRtOptions.cs           — コンパイルオプションビルダー
  LiteRtCompiledModel.cs     — 推論実行の高レベル API
  LiteRtTensorBuffer.cs      — バッファ管理 (Lock/Unlock/Dispose)
  GpuOptions.cs              — GPU 固有オプション
  CpuOptions.cs              — CPU 固有オプション
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

## 言語

- コードコメント・ドキュメント・コミットメッセージは日本語で記述する
