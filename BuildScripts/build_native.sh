#!/bin/bash

# Copyright 2025 ayutaz
# Licensed under the Apache License, Version 2.0
# See LICENSE file or http://www.apache.org/licenses/LICENSE-2.0

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

# ビルド成果物をコピー (cp -L でシンボリックリンクを解決)
mkdir -p "${OUTPUT_DIR}/Android/arm64-v8a"
SO_PATH="$(readlink -f bazel-bin/litert/c/libLiteRt.so)"
echo "成果物パス: ${SO_PATH}"
cp -L "${SO_PATH}" "${OUTPUT_DIR}/Android/arm64-v8a/libLiteRt.so"
ls -la "${OUTPUT_DIR}/Android/arm64-v8a/"

echo ""
echo "=== ビルド完了 ==="
