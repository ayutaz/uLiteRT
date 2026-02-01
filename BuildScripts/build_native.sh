#!/bin/bash
# Docker コンテナ内で実行される LiteRT C API ビルドスクリプト
# Android arm64 と Linux x86_64 の共有ライブラリをビルドする
set -euo pipefail

OUTPUT_DIR="${OUTPUT_DIR:-/output}"

echo "=== LiteRT C API ネイティブライブラリビルド ==="

# Android arm64
echo ""
echo "--- Android arm64 ビルド ---"
bazel build --config=android_arm64 //litert/c:litert_runtime_c_api_so
echo "Android arm64 ビルド完了"

# ビルド成果物をコピー
mkdir -p "${OUTPUT_DIR}/Android/arm64-v8a"
cp bazel-bin/litert/c/libLiteRt.so "${OUTPUT_DIR}/Android/arm64-v8a/" 2>/dev/null || \
    find bazel-bin/litert/c -name "*.so" -exec cp {} "${OUTPUT_DIR}/Android/arm64-v8a/" \;

echo ""
echo "=== ビルド完了 ==="
echo "成果物:"
find "${OUTPUT_DIR}" -type f \( -name "*.so" -o -name "*.dylib" \) | sort
