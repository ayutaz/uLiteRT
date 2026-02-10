# Changelog

本プロジェクトのすべての注目すべき変更はこのファイルに記録されます。
形式は [Keep a Changelog](https://keepachangelog.com/ja/1.1.0/) に基づいています。

## [0.2.0] - 2026-02-10

### Added
- サンプル 7 種 (画像分類/物体検出/セグメンテーション/姿勢推定/スタイル変換/音声分類/TTS)
- サンプル共通基盤 (SampleBase, ImageHelper, LabelLoader)
- GPU 優先選択 + CPU 自動フォールバック
- GPU 環境初期化 (LiteRtEnvironment.CreateGpuEnvironment)
- 動的形状テンソルバッファ対応 (EstimateDynamicBufferSize)
- モデル・ラベルダウンロードスクリプト (download_models.sh)
- サンプル共通基盤テスト (ImageHelper/LabelLoader 22テスト)
- CalculatePackedBufferSize テスト (21テスト)
- DisposeSafety テスト (16テスト)
- EstimateDynamicBufferSize テスト (11テスト)

### Changed
- TTS トークナイズを音素レベル→文字レベルに変更 (LJSpeechProcessor 互換)
- CMU dict 依存を削除

## [0.1.0] - 2026-02-01

### Added
- LiteRT 2.x CompiledModel C API の P/Invoke バインディング (13クラス)
- 構造体・列挙型・例外の C# 定義
- GPU/CPU/Runtime オプションクラス
- テンソルバッファ管理 (Lock/Unlock/Dispose)
- テンソルメタデータ取得 (LiteRtTensorInfo)
- Android arm64 ネイティブライブラリビルドスクリプト (Docker)
- Windows x86_64 ビルドスクリプト (Bazel)
- PlayMode テスト 6ファイル (40テスト)
