// Copyright 2025 ayutaz
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

// 画像分類サンプル — MobileNet V2

using System;
using UnityEngine;

namespace LiteRT.Samples
{
    /// <summary>
    /// MobileNet V2 による画像分類サンプル。
    /// 入力: float32 [1, 224, 224, 3] — [-1.0, 1.0]
    /// 出力: float32 [1, 1001] — ImageNet 1001 クラス確率
    /// </summary>
    public class ImageClassificationSample : SampleBase
    {
        private const int InputWidth = 224;
        private const int InputHeight = 224;
        private const string LabelFileName = "imagenet_labels.txt";

        [SerializeField] private Texture2D inputImage;

        private string[] _labels;
        private LiteRtTensorBuffer _inputBuffer;
        private LiteRtTensorBuffer _outputBuffer;

        private (int index, float probability)[] _topResults;
        private Texture2D _resizedPreview;

        protected override void Start()
        {
            modelFileName = "mobilenet_v2.tflite";
            base.Start();
            _labels = LabelLoader.LoadLabels(LabelFileName);
            if (_labels.Length > 0)
                Log($"ラベル読み込み完了: {_labels.Length} クラス");
        }

        protected override string GetSampleTitle()
        {
            return "画像分類 — MobileNet V2";
        }

        protected override void OnModelLoaded()
        {
            _inputBuffer?.Dispose();
            _outputBuffer?.Dispose();
            _inputBuffer = CreateInputBuffer();
            _outputBuffer = CreateOutputBuffer();

            var inputType = Model.GetInputTensorType(0);
            var dims = inputType.layout.GetDimensions();
            Log($"入力テンソル: [{string.Join(", ", dims)}] {inputType.elementType}");

            var outputType = Model.GetOutputTensorType(0);
            var outDims = outputType.layout.GetDimensions();
            Log($"出力テンソル: [{string.Join(", ", outDims)}] {outputType.elementType}");
        }

        protected override void DrawSampleGUI()
        {
            GUILayout.BeginHorizontal();

            // 画像プレビュー
            GUILayout.BeginVertical(GUILayout.Width(240));
            GUILayout.Label("入力画像:");
            if (inputImage != null)
            {
                GUILayout.Label(inputImage, GUILayout.Width(224), GUILayout.Height(224));
            }
            else
            {
                GUILayout.Box("Texture2D をインスペクタから設定してください", GUILayout.Width(224), GUILayout.Height(224));
            }
            GUILayout.EndVertical();

            GUILayout.Space(20);

            // 操作・結果
            GUILayout.BeginVertical();

            if (GUILayout.Button("推論実行", GUILayout.Width(120), GUILayout.Height(40)))
            {
                RunClassification();
            }

            if (_topResults != null)
            {
                GUILayout.Space(10);
                GUILayout.Label("--- Top-5 分類結果 ---");
                foreach (var (index, probability) in _topResults)
                {
                    string label = index < _labels.Length ? _labels[index] : $"クラス {index}";
                    GUILayout.Label($"  {label}: {probability:P2}");
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void RunClassification()
        {
            if (inputImage == null)
            {
                ErrorMessage = "入力画像が設定されていません。";
                return;
            }

            try
            {
                ErrorMessage = null;

                // 前処理: リサイズ → [-1, 1] 正規化
                var resized = ImageHelper.Resize(inputImage, InputWidth, InputHeight);

                // mean=0.5, std=0.5 で [0,1] → [-1,1] に変換
                var floatData = ImageHelper.TextureToFloatArray(resized, 0.5f, 0.5f);
                Destroy(resized);

                // テンソルに書き込み・推論
                _inputBuffer.WriteFloat(floatData);
                RunInference(
                    new[] { _inputBuffer },
                    new[] { _outputBuffer });

                // 結果読み取り
                var output = _outputBuffer.ReadFloat();
                _topResults = LabelLoader.GetTopK(output, 5);

                Log($"推論完了 ({LastInferenceMs:F2} ms) — Top-1: " +
                    (_topResults.Length > 0 && _topResults[0].index < _labels.Length
                        ? $"{_labels[_topResults[0].index]} ({_topResults[0].probability:P2})"
                        : "不明"));
            }
            catch (Exception e)
            {
                ErrorMessage = $"推論エラー: {e.Message}";
                Log(ErrorMessage);
            }
        }

        protected override void DisposeResources()
        {
            _inputBuffer?.Dispose();
            _inputBuffer = null;
            _outputBuffer?.Dispose();
            _outputBuffer = null;
            if (_resizedPreview != null)
            {
                Destroy(_resizedPreview);
                _resizedPreview = null;
            }
            base.DisposeResources();
        }
    }
}
