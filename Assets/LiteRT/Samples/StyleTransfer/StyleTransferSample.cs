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

// スタイル変換サンプル — Magenta

using System;
using UnityEngine;

namespace LiteRT.Samples
{
    /// <summary>
    /// Magenta Arbitrary Image Stylization によるスタイル変換サンプル。
    /// 2段階: Style Predict (スタイル画像 → ベクトル) → Style Transfer (コンテンツ + ベクトル → 出力)
    /// </summary>
    public class StyleTransferSample : SampleBase
    {
        private const int StyleSize = 256;
        private const int ContentSize = 384;
        private const string PredictModelFileName = "style_predict.tflite";
        private const string TransferModelFileName = "style_transfer.tflite";

        [SerializeField] private Texture2D contentImage;
        [SerializeField] private Texture2D styleImage;

        // 2つのモデルを管理
        private LiteRtModel _predictModel;
        private LiteRtCompiledModel _predictCompiled;
        private LiteRtOptions _predictOptions;

        // バッファ (Predict)
        private LiteRtTensorBuffer _predictInputBuffer;
        private LiteRtTensorBuffer _predictOutputBuffer;

        // バッファ (Transfer)
        private LiteRtTensorBuffer _transferContentBuffer;
        private LiteRtTensorBuffer _transferStyleBuffer;
        private LiteRtTensorBuffer _transferOutputBuffer;

        private Texture2D _resultTexture;
        private bool _isPredictLoaded;
        private bool _isTransferLoaded;

        protected override void Start()
        {
            // 最初に Transfer モデルをメインとしてロード
            modelFileName = TransferModelFileName;
            base.Start();
        }

        protected override string GetSampleTitle()
        {
            return "スタイル変換 — Magenta";
        }

        protected override void OnModelLoaded()
        {
            // Transfer モデルのバッファ作成
            _transferContentBuffer?.Dispose();
            _transferStyleBuffer?.Dispose();
            _transferOutputBuffer?.Dispose();

            // Transfer モデル: 入力0=コンテンツ画像, 入力1=スタイルベクトル
            _transferContentBuffer = CreateInputBuffer(0);
            _transferStyleBuffer = CreateInputBuffer(1);
            _transferOutputBuffer = CreateOutputBuffer(0);
            _isTransferLoaded = true;

            // Predict モデルも読み込む
            LoadPredictModel();
        }

        private void LoadPredictModel()
        {
            try
            {
                var predictPath = System.IO.Path.Combine(
                    Application.streamingAssetsPath, "LiteRT", "Models", PredictModelFileName);

                _predictModel = LiteRtModel.FromFile(predictPath);
                _predictOptions = new LiteRtOptions()
                    .SetHardwareAccelerators(selectedAccelerator);

                if ((selectedAccelerator & LiteRtHwAccelerators.Cpu) != 0)
                {
                    var cpuOpts = new CpuOptions();
                    cpuOpts.SetNumThreads(cpuThreadCount);
                    _predictOptions.AddCpuOptions(cpuOpts);
                }

                _predictCompiled = new LiteRtCompiledModel(Environment, _predictModel, _predictOptions);

                _predictInputBuffer?.Dispose();
                _predictOutputBuffer?.Dispose();

                _predictInputBuffer = LiteRtTensorBuffer.CreateFromRequirements(
                    Environment, _predictCompiled, _predictModel, 0, true);
                _predictOutputBuffer = LiteRtTensorBuffer.CreateFromRequirements(
                    Environment, _predictCompiled, _predictModel, 0, false);

                _isPredictLoaded = true;
                Log("Predict モデル読み込み完了");
            }
            catch (Exception e)
            {
                Log($"Predict モデル読み込みエラー: {e.Message}");
            }
        }

        protected override void DrawSampleGUI()
        {
            GUILayout.BeginHorizontal();

            // コンテンツ画像
            GUILayout.BeginVertical(GUILayout.Width(200));
            GUILayout.Label("コンテンツ画像:");
            if (contentImage != null)
                GUILayout.Label(contentImage, GUILayout.Width(192), GUILayout.Height(192));
            else
                GUILayout.Box("設定してください", GUILayout.Width(192), GUILayout.Height(192));
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // スタイル画像
            GUILayout.BeginVertical(GUILayout.Width(200));
            GUILayout.Label("スタイル画像:");
            if (styleImage != null)
                GUILayout.Label(styleImage, GUILayout.Width(192), GUILayout.Height(192));
            else
                GUILayout.Box("設定してください", GUILayout.Width(192), GUILayout.Height(192));
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // 結果画像
            GUILayout.BeginVertical(GUILayout.Width(200));
            GUILayout.Label("変換結果:");
            if (_resultTexture != null)
                GUILayout.Label(_resultTexture, GUILayout.Width(192), GUILayout.Height(192));
            else
                GUILayout.Box("推論実行後に表示", GUILayout.Width(192), GUILayout.Height(192));
            GUILayout.EndVertical();

            GUILayout.Space(20);

            // 操作
            GUILayout.BeginVertical();

            var canRun = _isPredictLoaded && _isTransferLoaded;
            GUI.enabled = canRun;
            if (GUILayout.Button("スタイル変換実行", GUILayout.Width(150), GUILayout.Height(40)))
            {
                RunStyleTransfer();
            }
            GUI.enabled = true;

            if (!canRun)
            {
                GUILayout.Label("両方のモデルを読み込んでください");
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void RunStyleTransfer()
        {
            if (contentImage == null || styleImage == null)
            {
                ErrorMessage = "コンテンツ画像とスタイル画像の両方を設定してください。";
                return;
            }

            try
            {
                ErrorMessage = null;

                // Step 1: Style Predict — スタイル画像からスタイルベクトルを抽出
                var resizedStyle = ImageHelper.Resize(styleImage, StyleSize, StyleSize);
                var styleData = ImageHelper.TextureToFloatArray(resizedStyle);
                Destroy(resizedStyle);

                _predictInputBuffer.WriteFloat(styleData);

                InferenceStopwatch.Restart();
                _predictCompiled.Run(
                    new[] { _predictInputBuffer },
                    new[] { _predictOutputBuffer });

                // スタイルベクトルを読み取り
                var styleVector = _predictOutputBuffer.ReadFloat();

                // Step 2: Style Transfer — コンテンツ画像 + スタイルベクトル → 変換画像
                var resizedContent = ImageHelper.Resize(contentImage, ContentSize, ContentSize);
                var contentData = ImageHelper.TextureToFloatArray(resizedContent);
                Destroy(resizedContent);

                _transferContentBuffer.WriteFloat(contentData);
                _transferStyleBuffer.WriteFloat(styleVector);

                CompiledModel.Run(
                    new[] { _transferContentBuffer, _transferStyleBuffer },
                    new[] { _transferOutputBuffer });
                InferenceStopwatch.Stop();
                LastInferenceMs = InferenceStopwatch.Elapsed.TotalMilliseconds;

                // 結果画像生成
                var outputData = _transferOutputBuffer.ReadFloat();
                if (_resultTexture != null) Destroy(_resultTexture);
                _resultTexture = ImageHelper.FloatArrayToTexture(outputData, ContentSize, ContentSize);

                Log($"スタイル変換完了 ({LastInferenceMs:F2} ms)");
            }
            catch (Exception e)
            {
                ErrorMessage = $"推論エラー: {e.Message}";
                Log(ErrorMessage);
            }
        }

        protected override void DisposeResources()
        {
            _predictInputBuffer?.Dispose();
            _predictInputBuffer = null;
            _predictOutputBuffer?.Dispose();
            _predictOutputBuffer = null;
            _transferContentBuffer?.Dispose();
            _transferContentBuffer = null;
            _transferStyleBuffer?.Dispose();
            _transferStyleBuffer = null;
            _transferOutputBuffer?.Dispose();
            _transferOutputBuffer = null;

            _predictCompiled?.Dispose();
            _predictCompiled = null;
            _predictOptions?.Dispose();
            _predictOptions = null;
            _predictModel?.Dispose();
            _predictModel = null;

            if (_resultTexture != null) Destroy(_resultTexture);
            _resultTexture = null;

            _isPredictLoaded = false;
            _isTransferLoaded = false;

            base.DisposeResources();
        }
    }
}
