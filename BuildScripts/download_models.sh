#!/bin/bash

# Copyright 2025 ayutaz
# Licensed under the Apache License, Version 2.0
# See LICENSE file or http://www.apache.org/licenses/LICENSE-2.0

# サンプル用モデル・ラベルファイルのダウンロードスクリプト
# Windows では Git Bash で実行: bash BuildScripts/download_models.sh
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
STREAMING_ASSETS="${PROJECT_DIR}/Assets/StreamingAssets/LiteRT"

MODELS_DIR="${STREAMING_ASSETS}/Models"
LABELS_DIR="${STREAMING_ASSETS}/Labels"
TTS_DIR="${STREAMING_ASSETS}/TTS"
TEMP_DIR="${PROJECT_DIR}/Temp/ModelDownload"

echo "=== サンプルモデル・ラベルファイル ダウンロード ==="
echo "配置先: ${STREAMING_ASSETS}"
echo ""

mkdir -p "${MODELS_DIR}" "${LABELS_DIR}" "${TTS_DIR}" "${TEMP_DIR}"

# ダウンロードヘルパー（既存ファイルはスキップ）
download_file() {
    local url="$1"
    local dest="$2"
    if [ -f "${dest}" ]; then
        echo "  スキップ（既存）: $(basename "${dest}")"
        return 0
    fi
    echo "  ダウンロード: $(basename "${dest}")"
    curl -sSL -o "${dest}" "${url}"
}

# --- モデルファイル ---
echo "--- モデルファイル ---"

# 1. MobileNet V2
if [ ! -f "${MODELS_DIR}/mobilenet_v2.tflite" ]; then
    echo "  ダウンロード: mobilenet_v2.tflite"
    curl -sSL -o "${TEMP_DIR}/mobilenet_v2.tgz" \
        "https://storage.googleapis.com/download.tensorflow.org/models/tflite_11_05_08/mobilenet_v2_1.0_224.tgz"
    tar xzf "${TEMP_DIR}/mobilenet_v2.tgz" -C "${TEMP_DIR}"
    cp "${TEMP_DIR}/mobilenet_v2_1.0_224.tflite" "${MODELS_DIR}/mobilenet_v2.tflite"
else
    echo "  スキップ（既存）: mobilenet_v2.tflite"
fi

# 2. SSD MobileNet V1
if [ ! -f "${MODELS_DIR}/ssd_mobilenet_v1.tflite" ]; then
    echo "  ダウンロード: ssd_mobilenet_v1.tflite"
    curl -sSL -o "${TEMP_DIR}/ssd_mobilenet.zip" \
        "https://storage.googleapis.com/download.tensorflow.org/models/tflite/coco_ssd_mobilenet_v1_1.0_quant_2018_06_29.zip"
    unzip -qo "${TEMP_DIR}/ssd_mobilenet.zip" -d "${TEMP_DIR}/ssd_mobilenet"
    cp "${TEMP_DIR}/ssd_mobilenet/detect.tflite" "${MODELS_DIR}/ssd_mobilenet_v1.tflite"
else
    echo "  スキップ（既存）: ssd_mobilenet_v1.tflite"
fi

# 3. DeepLab V3
download_file \
    "https://tfhub.dev/tensorflow/lite-model/deeplabv3/1/metadata/2?lite-format=tflite" \
    "${MODELS_DIR}/deeplabv3.tflite"

# 4. PoseNet
download_file \
    "https://storage.googleapis.com/download.tensorflow.org/models/tflite/posenet_mobilenet_v1_100_257x257_multi_kpt_stripped.tflite" \
    "${MODELS_DIR}/posenet.tflite"

# 5. Style Predict
download_file \
    "https://tfhub.dev/google/lite-model/magenta/arbitrary-image-stylization-v1-256/int8/prediction/1?lite-format=tflite" \
    "${MODELS_DIR}/style_predict.tflite"

# 6. Style Transfer
download_file \
    "https://tfhub.dev/google/lite-model/magenta/arbitrary-image-stylization-v1-256/int8/transfer/1?lite-format=tflite" \
    "${MODELS_DIR}/style_transfer.tflite"

# 7. YAMNet
download_file \
    "https://tfhub.dev/google/lite-model/yamnet/classification/tflite/1?lite-format=tflite" \
    "${MODELS_DIR}/yamnet.tflite"

# 8. FastSpeech2 + MB-MelGAN
if [ ! -f "${MODELS_DIR}/fastspeech2.tflite" ] || [ ! -f "${MODELS_DIR}/mb_melgan.tflite" ]; then
    echo "  ダウンロード: fastspeech2.tflite + mb_melgan.tflite"
    curl -sSL -o "${TEMP_DIR}/tts_models.zip" \
        "https://github.com/luan78zaoha/TTS_tflite_cpp/releases/download/0.1.0/models.zip"
    unzip -qo "${TEMP_DIR}/tts_models.zip" -d "${TEMP_DIR}/tts_models"
    # zip 内のファイル名を探してコピー
    find "${TEMP_DIR}/tts_models" -name "*fastspeech2*.tflite" -exec cp {} "${MODELS_DIR}/fastspeech2.tflite" \;
    find "${TEMP_DIR}/tts_models" -name "*mb_melgan*.tflite" -exec cp {} "${MODELS_DIR}/mb_melgan.tflite" \;
else
    echo "  スキップ（既存）: fastspeech2.tflite + mb_melgan.tflite"
fi

echo ""

# --- ラベルファイル ---
echo "--- ラベルファイル ---"

# ImageNet ラベル
download_file \
    "https://storage.googleapis.com/download.tensorflow.org/data/ImageNetLabels.txt" \
    "${LABELS_DIR}/imagenet_labels.txt"

# COCO ラベル
download_file \
    "https://raw.githubusercontent.com/android/camera-samples/main/CameraXAdvanced/tflite/src/main/assets/coco_ssd_mobilenet_v1_1.0_labels.txt" \
    "${LABELS_DIR}/coco_labels.txt"

# AudioSet ラベル (YAMNet)
download_file \
    "https://raw.githubusercontent.com/tensorflow/models/master/research/audioset/yamnet/yamnet_class_map.csv" \
    "${LABELS_DIR}/yamnet_class_map.csv"

echo ""

# --- TTS 関連 ---
echo "--- TTS 関連ファイル ---"

# LJSpeech mapper (Google Drive)
download_file \
    "https://drive.google.com/uc?export=download&id=1YBaDdMlhTXxsKrH7mZwDu-2aODq5fr5e" \
    "${TTS_DIR}/ljspeech_mapper.json"

echo ""

# テンポラリディレクトリ削除
rm -rf "${TEMP_DIR}"

echo "=== 完了 ==="
echo ""
echo "配置済みファイル:"
find "${STREAMING_ASSETS}" -type f | sort
