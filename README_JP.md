# uLiteRT

[![Unity](https://img.shields.io/badge/Unity-6000.3+-black.svg)](https://unity.com/)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![LiteRT](https://img.shields.io/badge/LiteRT-v2.1.2-orange.svg)](https://github.com/google-ai-edge/LiteRT)

[Google LiteRT](https://github.com/google-ai-edge/LiteRT)（旧 TensorFlow Lite）の Unity 向け C# バインディング（P/Invoke 経由）

[English](README.md)

## 概要

uLiteRT は、LiteRT 2.x の CompiledModel C API を P/Invoke で直接呼び出す C# バインディングを提供します。共通の C# コードベースで全プラットフォームに対応し、ネイティブライブラリ（.dll/.so/.dylib）のみがプラットフォームごとに異なります。

## 特徴

- **P/Invoke ベース** — プラットフォーム非依存の C# コードとプラットフォーム別ネイティブライブラリ
- **ハードウェアアクセラレーション** — CPU / GPU / NPU 対応
- **IDisposable によるリソース管理** — ネイティブリソースの確定的な解放
- **ビルダーパターンのオプション設定** — GPU / CPU / Runtime オプション構成
- **同期・非同期推論** — `Run` および `RunAsync` API
- **テンソルバッファ管理** — Lock/Unlock パターンによるマネージド・ホストメモリバッファ

## 対応プラットフォーム

| プラットフォーム | アーキテクチャ | 状態 |
|---|---|---|
| Android | arm64-v8a | 検証済み (v2.1.2) |
| Windows | x86_64 | ビルドスクリプト準備済み |
| macOS | Apple Silicon | 予定 |
| iOS | arm64 | 予定 |

## 動作環境

- Unity 6000.3.2f1 以降 (Unity 6 LTS)
- .NET 4.7.1

## インストール

### Unity Package Manager (git URL)

1. **Window → Package Manager** を開く
2. **+** → **Add package from git URL** をクリック
3. 以下を入力:

```
https://github.com/ayutaz/uLiteRT.git?path=Assets/LiteRT
```

> **注意:** ネイティブライブラリはリポジトリに含まれていません。ターゲットプラットフォーム向けのビルド方法は[ネイティブライブラリのビルド](#ネイティブライブラリのビルド)を参照してください。

## クイックスタート

```csharp
using LiteRT;

// 1. 環境の作成
using var environment = new LiteRtEnvironment();

// 2. モデルの読み込み
using var model = LiteRtModel.FromFile(modelPath);

// 3. オプションの設定
using var options = new LiteRtOptions();
options.SetHardwareAccelerators(LiteRtHwAccelerators.kLiteRtHwAcceleratorCpu);

// 4. モデルのコンパイル
using var compiledModel = new LiteRtCompiledModel(environment, model, options);

// 5. モデル要件から I/O バッファを作成
using var inputBuffer = LiteRtTensorBuffer.CreateFromRequirements(
    environment, compiledModel, model,
    tensorIndex: 0, isInput: true);
using var outputBuffer = LiteRtTensorBuffer.CreateFromRequirements(
    environment, compiledModel, model,
    tensorIndex: 0, isInput: false);

// 6. 入力データの書き込み
inputBuffer.WriteFloat(inputData);

// 7. 推論の実行
compiledModel.Run(
    new[] { inputBuffer },
    new[] { outputBuffer });

// 8. 出力の読み取り
float[] result = outputBuffer.ReadFloat();
```

## API 概要

| クラス | 説明 |
|---|---|
| `LiteRtEnvironment` | ランタイム環境の初期化 |
| `LiteRtModel` | モデルの読み込みとシグネチャ情報 |
| `LiteRtOptions` | コンパイルオプションビルダー |
| `LiteRtCompiledModel` | 推論実行（同期/非同期） |
| `LiteRtTensorBuffer` | I/O バッファ管理（Lock/Unlock） |
| `GpuOptions` | GPU 固有の設定 |
| `CpuOptions` | CPU 固有の設定 |
| `RuntimeOptions` | ランタイム設定 |
| `LiteRtTensorInfo` | テンソルメタデータ（読み取り専用） |

### API 呼び出しフロー

```
初期化:  Environment → Model → Options → CompiledModel → TensorBuffer
推論:    WriteFloat(入力) → Run → ReadFloat(出力)
破棄:    TensorBuffer → CompiledModel → Options → Model → Environment（逆順）
```

## ネイティブライブラリのビルド

ネイティブライブラリはターゲットプラットフォームごとに個別にビルドする必要があります。

### 前提条件

| プラットフォーム | 必要なもの |
|---|---|
| Android | Docker |
| Windows | Bazel |

### Android (arm64)

```bash
bash BuildScripts/build_all.sh
```

出力先: `Assets/Plugins/Android/arm64-v8a/libLiteRt.so`

### Windows (x86_64)

```bat
BuildScripts\build_native.bat
```

出力先: `Assets/Plugins/Windows/x86_64/libLiteRt.dll`

> LiteRT のソースコードは本リポジトリの相対パス `../LiteRT/` に配置されている前提です。現在 LiteRT **v2.1.2** タグで検証済みです。

## ライセンス

本プロジェクトは [Apache License 2.0](LICENSE) の下でライセンスされています。

uLiteRT はサードパーティのバインディングライブラリであり、Google とは提携・推奨関係にありません。
[LiteRT](https://github.com/google-ai-edge/LiteRT) は Google AI Edge チームにより Apache License 2.0 の下で開発されています。
