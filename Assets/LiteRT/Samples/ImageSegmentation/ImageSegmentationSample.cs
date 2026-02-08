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

// 画像セグメンテーションサンプル — DeepLab V3

using System;
using UnityEngine;

namespace LiteRT.Samples
{
    /// <summary>
    /// DeepLab V3 による画像セグメンテーションサンプル。
    /// 入力: float32 [1, 257, 257, 3] — [0, 1]
    /// 出力: float32 [1, 257, 257, 21] — 21 クラスマップ
    /// </summary>
    public class ImageSegmentationSample : SampleBase
    {
        private const int InputWidth = 257;
        private const int InputHeight = 257;
        private const int NumClasses = 21;

        private static readonly string[] ClassNames =
        {
            "背景", "飛行機", "自転車", "鳥", "船",
            "ボトル", "バス", "車", "猫", "椅子",
            "牛", "テーブル", "犬", "馬", "バイク",
            "人", "植木鉢", "羊", "ソファ", "列車", "テレビ"
        };

        [SerializeField] private Texture2D inputImage;
        [SerializeField] [Range(0f, 1f)] private float overlayAlpha = 0.5f;

        private LiteRtTensorBuffer _inputBuffer;
        private LiteRtTensorBuffer _outputBuffer;

        private Texture2D _maskTexture;

        protected override void Start()
        {
            modelFileName = "deeplabv3.tflite";
            base.Start();
        }

        protected override string GetSampleTitle()
        {
            return "画像セグメンテーション — DeepLab V3";
        }

        protected override void OnModelLoaded()
        {
            _inputBuffer?.Dispose();
            _outputBuffer?.Dispose();
            _inputBuffer = CreateInputBuffer();
            _outputBuffer = CreateOutputBuffer();
        }

        protected override void DrawSampleGUI()
        {
            GUILayout.BeginHorizontal();

            // 元画像
            GUILayout.BeginVertical(GUILayout.Width(270));
            GUILayout.Label("元画像:");
            if (inputImage != null)
                GUILayout.Label(inputImage, GUILayout.Width(257), GUILayout.Height(257));
            else
                GUILayout.Box("Texture2D を設定", GUILayout.Width(257), GUILayout.Height(257));
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // セグメンテーションマスク
            GUILayout.BeginVertical(GUILayout.Width(270));
            GUILayout.Label("セグメンテーションマスク:");
            if (_maskTexture != null)
            {
                var maskRect = GUILayoutUtility.GetRect(257, 257);
                // 元画像を下に表示
                if (inputImage != null)
                    GUI.DrawTexture(maskRect, inputImage, ScaleMode.ScaleToFit);
                // マスクを上にオーバーレイ
                var prevColor = GUI.color;
                GUI.color = new Color(1, 1, 1, overlayAlpha);
                GUI.DrawTexture(maskRect, _maskTexture, ScaleMode.ScaleToFit);
                GUI.color = prevColor;
            }
            else
            {
                GUILayout.Box("推論実行後に表示", GUILayout.Width(257), GUILayout.Height(257));
            }
            GUILayout.EndVertical();

            GUILayout.Space(20);

            // 操作
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"透明度: {overlayAlpha:F2}", GUILayout.Width(90));
            overlayAlpha = GUILayout.HorizontalSlider(overlayAlpha, 0f, 1f, GUILayout.Width(200));
            GUILayout.EndHorizontal();

            if (GUILayout.Button("推論実行", GUILayout.Width(120), GUILayout.Height(40)))
            {
                RunSegmentation();
            }

            // 凡例
            GUILayout.Space(10);
            GUILayout.Label("--- クラス凡例 ---");
            for (int i = 0; i < ClassNames.Length; i++)
            {
                float hue = (float)i / NumClasses;
                var color = Color.HSVToRGB(hue, 0.8f, 0.9f);
                var style = new GUIStyle(GUI.skin.label) { normal = { textColor = color } };
                GUILayout.Label($"  [{i:D2}] {ClassNames[i]}", style);
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void RunSegmentation()
        {
            if (inputImage == null)
            {
                ErrorMessage = "入力画像が設定されていません。";
                return;
            }

            try
            {
                ErrorMessage = null;

                // 前処理: リサイズ → [0, 1] 正規化
                var resized = ImageHelper.Resize(inputImage, InputWidth, InputHeight);
                var floatData = ImageHelper.TextureToFloatArray(resized);
                Destroy(resized);

                _inputBuffer.WriteFloat(floatData);
                RunInference(
                    new[] { _inputBuffer },
                    new[] { _outputBuffer });

                // 出力: [1, 257, 257, 21] → argmax で各ピクセルのクラスを決定
                var output = _outputBuffer.ReadFloat();
                var classIds = new int[InputWidth * InputHeight];

                for (int i = 0; i < InputWidth * InputHeight; i++)
                {
                    int maxClass = 0;
                    float maxVal = output[i * NumClasses];
                    for (int c = 1; c < NumClasses; c++)
                    {
                        float val = output[i * NumClasses + c];
                        if (val > maxVal)
                        {
                            maxVal = val;
                            maxClass = c;
                        }
                    }
                    classIds[i] = maxClass;
                }

                // マスクテクスチャ生成
                if (_maskTexture != null) Destroy(_maskTexture);
                _maskTexture = ImageHelper.ClassMapToTexture(classIds, InputWidth, InputHeight, NumClasses);

                Log($"推論完了 ({LastInferenceMs:F2} ms)");
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
            if (_maskTexture != null) Destroy(_maskTexture);
            _maskTexture = null;
            base.DisposeResources();
        }
    }
}
