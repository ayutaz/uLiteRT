#!/bin/bash
# LiteRT C API ネイティブライブラリの Docker ビルドを起動し、
# 成果物を Assets/Plugins/ にコピーするスクリプト
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
LITERT_DIR="$(cd "${SCRIPT_DIR}/../../LiteRT" && pwd)"
PLUGINS_DIR="${PROJECT_DIR}/Assets/Plugins"

IMAGE_NAME="ulitert_build"
CONTAINER_NAME="ulitert_build_container"

echo "=== uLiteRT ネイティブライブラリビルド ==="
echo "LiteRT ソース: ${LITERT_DIR}"
echo "出力先: ${PLUGINS_DIR}"
echo ""

# LiteRT ソースの存在確認
if [ ! -d "${LITERT_DIR}" ]; then
    echo "エラー: LiteRT ソースが見つかりません: ${LITERT_DIR}"
    echo "LiteRT リポジトリを ${LITERT_DIR} に配置してください"
    exit 1
fi

# Docker イメージビルド
echo "--- Docker イメージビルド ---"
docker build \
    -t "${IMAGE_NAME}" \
    -f "${SCRIPT_DIR}/Dockerfile" \
    "${PROJECT_DIR}"

# 既存コンテナを削除
if docker ps -a --format '{{.Names}}' | grep -q "^${CONTAINER_NAME}$"; then
    echo "既存コンテナを削除: ${CONTAINER_NAME}"
    docker rm -f "${CONTAINER_NAME}"
fi

# 出力ディレクトリ作成
mkdir -p "${PLUGINS_DIR}/Android/arm64-v8a"

# Docker ビルド実行
echo ""
echo "--- Docker ビルド実行 ---"

# macOS Apple Silicon の場合は SVE を無効化
EXTRA_ARGS=()
if [ "$(uname -s)" = "Darwin" ] && [ "$(uname -m)" = "arm64" ]; then
    EXTRA_ARGS+=(-e DISABLE_SVE_FOR_BAZEL=1)
fi

docker run \
    --name "${CONTAINER_NAME}" \
    --security-opt seccomp=unconfined \
    -e HOME=/litert_build \
    -e OUTPUT_DIR=/output \
    "${EXTRA_ARGS[@]+"${EXTRA_ARGS[@]}"}" \
    -v "${LITERT_DIR}:/litert_build" \
    -v "${PLUGINS_DIR}:/output" \
    "${IMAGE_NAME}"

BUILD_STATUS=$?

if [ ${BUILD_STATUS} -ne 0 ]; then
    echo "エラー: ビルドに失敗しました"
    exit 1
fi

# コンテナ削除
docker rm -f "${CONTAINER_NAME}" > /dev/null 2>&1 || true

echo ""
echo "=== 完了 ==="
echo "成果物:"
find "${PLUGINS_DIR}" -type f \( -name "*.so" -o -name "*.dll" -o -name "*.dylib" \) | sort
echo ""
echo "※ Windows DLL は BuildScripts/build_native.bat を Windows 上で実行してください"
