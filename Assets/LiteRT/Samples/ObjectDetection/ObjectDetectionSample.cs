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

// 物体検出サンプル — SSD MobileNet V1

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LiteRT.Samples
{
    /// <summary>
    /// SSD MobileNet V1 による物体検出サンプル。
    /// 入力: uint8 [1, 300, 300, 3] — [0, 255]
    /// 出力: boxes [1, 10, 4], classes [1, 10], scores [1, 10], num_detections [1]
    /// </summary>
    public class ObjectDetectionSample : SampleBase
    {
        private const int InputWidth = 300;
        private const int InputHeight = 300;
        private const string LabelFileName = "coco_labels.txt";

        [SerializeField] private Texture2D inputImage;
        [SerializeField] [Range(0.1f, 1.0f)] private float scoreThreshold = 0.5f;

        private string[] _labels;
        private LiteRtTensorBuffer _inputBuffer;
        private LiteRtTensorBuffer[] _outputBuffers;

        private List<Detection> _detections = new List<Detection>();

        private struct Detection
        {
            public Rect box;
            public int classId;
            public float score;
        }

        protected override void Start()
        {
            modelFileName = "ssd_mobilenet_v1.tflite";
            base.Start();
            _labels = LabelLoader.LoadLabels(LabelFileName);
            if (_labels.Length > 0)
                Log($"ラベル読み込み完了: {_labels.Length} クラス");
        }

        protected override string GetSampleTitle()
        {
            return "物体検出 — SSD MobileNet V1";
        }

        // SSD MobileNet V1 の既知の出力 float 数（動的形状モデル対応）
        // boxes[1,10,4]=40, classes[1,10]=10, scores[1,10]=10, num_detections[1]=1
        private static readonly int[] ExpectedOutputFloats = { 40, 10, 10, 1 };

        protected override void OnModelLoaded()
        {
            _inputBuffer?.Dispose();
            if (_outputBuffers != null)
                foreach (var buf in _outputBuffers) buf?.Dispose();

            _inputBuffer = CreateInputBuffer();

            // SSD MobileNet V1 は4つの出力テンソルを持つ
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

            // 画像プレビュー（ボックスオーバーレイ付き）
            GUILayout.BeginVertical(GUILayout.Width(320));
            GUILayout.Label("入力画像:");
            if (inputImage != null)
            {
                var imageRect = GUILayoutUtility.GetRect(300, 300);
                GUI.DrawTexture(imageRect, inputImage, ScaleMode.ScaleToFit);

                // バウンディングボックス描画
                foreach (var det in _detections)
                {
                    DrawBoundingBox(imageRect, det);
                }
            }
            else
            {
                GUILayout.Box("Texture2D をインスペクタから設定してください", GUILayout.Width(300), GUILayout.Height(300));
            }
            GUILayout.EndVertical();

            GUILayout.Space(20);

            // 操作
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"閾値: {scoreThreshold:F2}", GUILayout.Width(80));
            scoreThreshold = GUILayout.HorizontalSlider(scoreThreshold, 0.1f, 1.0f, GUILayout.Width(200));
            GUILayout.EndHorizontal();

            if (GUILayout.Button("推論実行", GUILayout.Width(120), GUILayout.Height(40)))
            {
                RunDetection();
            }

            if (_detections.Count > 0)
            {
                GUILayout.Space(10);
                GUILayout.Label($"--- 検出結果 ({_detections.Count} 件) ---");
                foreach (var det in _detections)
                {
                    string label = det.classId < _labels.Length ? _labels[det.classId] : $"クラス {det.classId}";
                    GUILayout.Label($"  {label}: {det.score:P2}");
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void RunDetection()
        {
            if (inputImage == null)
            {
                ErrorMessage = "入力画像が設定されていません。";
                return;
            }

            try
            {
                ErrorMessage = null;
                _detections.Clear();

                // 前処理: リサイズ → byte[]
                var resized = ImageHelper.Resize(inputImage, InputWidth, InputHeight);
                var byteData = ImageHelper.TextureToByteArray(resized);
                Destroy(resized);

                _inputBuffer.WriteBytes(byteData);
                RunInference(
                    new[] { _inputBuffer },
                    _outputBuffers);

                // 出力解析: boxes[1,10,4], classes[1,10], scores[1,10], num_detections[1]
                // 動的形状モデルでは PackedSize が実サイズより小さいため
                // 期待される要素数を明示して Lock/Marshal.Copy で読む
                var boxes = ReadOutputFloat(_outputBuffers[0], ExpectedOutputFloats[0]);
                var classes = ReadOutputFloat(_outputBuffers[1], ExpectedOutputFloats[1]);
                var scores = ReadOutputFloat(_outputBuffers[2], ExpectedOutputFloats[2]);
                var numDetections = ReadOutputFloat(_outputBuffers[3], ExpectedOutputFloats[3]);

                int count = Mathf.Min((int)numDetections[0], 10);
                for (int i = 0; i < count; i++)
                {
                    if (scores[i] < scoreThreshold) continue;

                    _detections.Add(new Detection
                    {
                        // boxes は [ymin, xmin, ymax, xmax] 正規化座標
                        box = new Rect(
                            boxes[i * 4 + 1], boxes[i * 4 + 0],
                            boxes[i * 4 + 3] - boxes[i * 4 + 1],
                            boxes[i * 4 + 2] - boxes[i * 4 + 0]),
                        classId = (int)classes[i],
                        score = scores[i]
                    });
                }

                Log($"推論完了 ({LastInferenceMs:F2} ms) — {_detections.Count} 件検出");
            }
            catch (Exception e)
            {
                ErrorMessage = $"推論エラー: {e.Message}";
                Log(ErrorMessage);
            }
        }

        private static float[] ReadOutputFloat(LiteRtTensorBuffer buffer, int expectedCount)
        {
            IntPtr ptr = buffer.Lock(LiteRtTensorBufferLockMode.Read);
            try
            {
                var result = new float[expectedCount];
                Marshal.Copy(ptr, result, 0, expectedCount);
                return result;
            }
            finally
            {
                buffer.Unlock();
            }
        }

        private void DrawBoundingBox(Rect imageRect, Detection det)
        {
            var boxRect = new Rect(
                imageRect.x + det.box.x * imageRect.width,
                imageRect.y + det.box.y * imageRect.height,
                det.box.width * imageRect.width,
                det.box.height * imageRect.height);

            // 枠描画（IMGUI の簡易実装）
            var color = Color.green;
            DrawRect(boxRect, color, 2);

            string label = det.classId < _labels.Length ? _labels[det.classId] : $"ID:{det.classId}";
            var labelStyle = new GUIStyle(GUI.skin.label) { normal = { textColor = color }, fontSize = 12 };
            GUI.Label(new Rect(boxRect.x, boxRect.y - 16, 200, 20), $"{label} {det.score:P0}", labelStyle);
        }

        private static void DrawRect(Rect rect, Color color, int thickness)
        {
            var tex = Texture2D.whiteTexture;
            var prevColor = GUI.color;
            GUI.color = color;
            // 上辺
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), tex);
            // 下辺
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), tex);
            // 左辺
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), tex);
            // 右辺
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), tex);
            GUI.color = prevColor;
        }

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
