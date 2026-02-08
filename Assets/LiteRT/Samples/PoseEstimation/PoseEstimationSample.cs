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

// 姿勢推定サンプル — PoseNet MobileNet

using System;
using UnityEngine;

namespace LiteRT.Samples
{
    /// <summary>
    /// PoseNet MobileNet による姿勢推定サンプル。
    /// 入力: float32 [1, 257, 257, 3] — [-1.0, 1.0]
    /// 出力: heatmaps [1, 9, 9, 17] + offsets [1, 9, 9, 34]
    /// </summary>
    public class PoseEstimationSample : SampleBase
    {
        private const int InputWidth = 257;
        private const int InputHeight = 257;
        private const int HeatmapSize = 9;
        private const int NumKeypoints = 17;

        private static readonly string[] KeypointNames =
        {
            "鼻", "左目", "右目", "左耳", "右耳",
            "左肩", "右肩", "左肘", "右肘", "左手首", "右手首",
            "左腰", "右腰", "左膝", "右膝", "左足首", "右足首"
        };

        // スケルトン接続定義 (from, to)
        private static readonly int[][] Skeleton =
        {
            new[] {0, 1}, new[] {0, 2}, new[] {1, 3}, new[] {2, 4},     // 顔
            new[] {5, 6},                                                   // 肩
            new[] {5, 7}, new[] {7, 9},                                    // 左腕
            new[] {6, 8}, new[] {8, 10},                                   // 右腕
            new[] {5, 11}, new[] {6, 12},                                  // 胴体
            new[] {11, 12},                                                // 腰
            new[] {11, 13}, new[] {13, 15},                                // 左脚
            new[] {12, 14}, new[] {14, 16},                                // 右脚
        };

        [SerializeField] private Texture2D inputImage;
        [SerializeField] [Range(0.1f, 1.0f)] private float confidenceThreshold = 0.3f;

        private LiteRtTensorBuffer _inputBuffer;
        private LiteRtTensorBuffer[] _outputBuffers;

        private Vector2[] _keypoints;
        private float[] _keypointScores;

        protected override void Start()
        {
            modelFileName = "posenet.tflite";
            base.Start();
        }

        protected override string GetSampleTitle()
        {
            return "姿勢推定 — PoseNet MobileNet";
        }

        protected override void OnModelLoaded()
        {
            _inputBuffer?.Dispose();
            if (_outputBuffers != null)
                foreach (var buf in _outputBuffers) buf?.Dispose();

            _inputBuffer = CreateInputBuffer();

            int numOutputs = Model.GetNumOutputs();
            _outputBuffers = new LiteRtTensorBuffer[numOutputs];
            for (int i = 0; i < numOutputs; i++)
            {
                _outputBuffers[i] = CreateOutputBuffer(i);
                var outType = Model.GetOutputTensorType(i);
                var dims = outType.layout.GetDimensions();
                Log($"出力[{i}]: [{string.Join(", ", dims)}] {outType.elementType}");
            }
        }

        protected override void DrawSampleGUI()
        {
            GUILayout.BeginHorizontal();

            // 画像+スケルトン表示
            GUILayout.BeginVertical(GUILayout.Width(280));
            GUILayout.Label("入力画像 + スケルトン:");
            if (inputImage != null)
            {
                var imageRect = GUILayoutUtility.GetRect(257, 257);
                GUI.DrawTexture(imageRect, inputImage, ScaleMode.ScaleToFit);

                // スケルトン描画
                if (_keypoints != null)
                {
                    DrawSkeleton(imageRect);
                }
            }
            else
            {
                GUILayout.Box("Texture2D を設定", GUILayout.Width(257), GUILayout.Height(257));
            }
            GUILayout.EndVertical();

            GUILayout.Space(20);

            // 操作・結果
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"信頼度閾値: {confidenceThreshold:F2}", GUILayout.Width(120));
            confidenceThreshold = GUILayout.HorizontalSlider(confidenceThreshold, 0.1f, 1.0f, GUILayout.Width(200));
            GUILayout.EndHorizontal();

            if (GUILayout.Button("推論実行", GUILayout.Width(120), GUILayout.Height(40)))
            {
                RunPoseEstimation();
            }

            if (_keypoints != null)
            {
                GUILayout.Space(10);
                GUILayout.Label("--- キーポイント ---");
                for (int i = 0; i < NumKeypoints; i++)
                {
                    if (_keypointScores[i] >= confidenceThreshold)
                    {
                        GUILayout.Label($"  {KeypointNames[i]}: ({_keypoints[i].x:F1}, {_keypoints[i].y:F1}) [{_keypointScores[i]:P0}]");
                    }
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void RunPoseEstimation()
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
                var floatData = ImageHelper.TextureToFloatArray(resized, 0.5f, 0.5f);
                Destroy(resized);

                _inputBuffer.WriteFloat(floatData);
                RunInference(
                    new[] { _inputBuffer },
                    _outputBuffers);

                // heatmaps [1, 9, 9, 17], offsets [1, 9, 9, 34]
                var heatmaps = _outputBuffers[0].ReadFloat();
                var offsets = _outputBuffers[1].ReadFloat();

                _keypoints = new Vector2[NumKeypoints];
                _keypointScores = new float[NumKeypoints];

                for (int k = 0; k < NumKeypoints; k++)
                {
                    // ヒートマップから最大位置を探す
                    float maxVal = float.MinValue;
                    int maxY = 0, maxX = 0;
                    for (int y = 0; y < HeatmapSize; y++)
                    {
                        for (int x = 0; x < HeatmapSize; x++)
                        {
                            int idx = (y * HeatmapSize + x) * NumKeypoints + k;
                            if (heatmaps[idx] > maxVal)
                            {
                                maxVal = heatmaps[idx];
                                maxY = y;
                                maxX = x;
                            }
                        }
                    }

                    _keypointScores[k] = Sigmoid(maxVal);

                    // オフセット適用
                    int offsetIdx = (maxY * HeatmapSize + maxX) * NumKeypoints * 2;
                    float offsetY = offsets[offsetIdx + k];
                    float offsetX = offsets[offsetIdx + NumKeypoints + k];

                    // ヒートマップ座標 → 画像座標に変換
                    float posY = (maxY * (float)InputHeight / (HeatmapSize - 1) + offsetY) / InputHeight;
                    float posX = (maxX * (float)InputWidth / (HeatmapSize - 1) + offsetX) / InputWidth;

                    _keypoints[k] = new Vector2(
                        Mathf.Clamp01(posX),
                        Mathf.Clamp01(posY));
                }

                int detected = 0;
                for (int i = 0; i < NumKeypoints; i++)
                    if (_keypointScores[i] >= confidenceThreshold) detected++;

                Log($"推論完了 ({LastInferenceMs:F2} ms) — {detected}/{NumKeypoints} キーポイント検出");
            }
            catch (Exception e)
            {
                ErrorMessage = $"推論エラー: {e.Message}";
                Log(ErrorMessage);
            }
        }

        private void DrawSkeleton(Rect imageRect)
        {
            // キーポイント描画
            for (int i = 0; i < NumKeypoints; i++)
            {
                if (_keypointScores[i] < confidenceThreshold) continue;
                var pos = new Vector2(
                    imageRect.x + _keypoints[i].x * imageRect.width,
                    imageRect.y + _keypoints[i].y * imageRect.height);

                var prevColor = GUI.color;
                GUI.color = Color.red;
                GUI.DrawTexture(new Rect(pos.x - 4, pos.y - 4, 8, 8), Texture2D.whiteTexture);
                GUI.color = prevColor;
            }

            // スケルトン接続描画
            foreach (var bone in Skeleton)
            {
                if (_keypointScores[bone[0]] < confidenceThreshold ||
                    _keypointScores[bone[1]] < confidenceThreshold) continue;

                var from = new Vector2(
                    imageRect.x + _keypoints[bone[0]].x * imageRect.width,
                    imageRect.y + _keypoints[bone[0]].y * imageRect.height);
                var to = new Vector2(
                    imageRect.x + _keypoints[bone[1]].x * imageRect.width,
                    imageRect.y + _keypoints[bone[1]].y * imageRect.height);

                DrawLine(from, to, Color.cyan, 2);
            }
        }

        private static void DrawLine(Vector2 from, Vector2 to, Color color, int thickness)
        {
            var prevColor = GUI.color;
            GUI.color = color;
            var dir = (to - from).normalized;
            float length = Vector2.Distance(from, to);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            var matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, from);
            GUI.DrawTexture(new Rect(from.x, from.y - thickness / 2f, length, thickness), Texture2D.whiteTexture);
            GUI.matrix = matrix;
            GUI.color = prevColor;
        }

        private static float Sigmoid(float x) => 1f / (1f + Mathf.Exp(-x));

        protected override void DisposeResources()
        {
            _inputBuffer?.Dispose();
            _inputBuffer = null;
            if (_outputBuffers != null)
            {
                foreach (var buf in _outputBuffers) buf?.Dispose();
                _outputBuffers = null;
            }
            base.DisposeResources();
        }
    }
}
