# Changelog

本プロジェクトのすべての注目すべき変更はこのファイルに記録されます。
形式は [Keep a Changelog](https://keepachangelog.com/ja/1.1.0/) に基づいています。

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
