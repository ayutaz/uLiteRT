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

// サンプル共通基底クラス

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LiteRT.Samples
{
    /// <summary>
    /// LiteRT サンプルの共通基底クラス。
    /// LiteRT パイプライン管理、IMGUI ヘルパー、診断ログを提供する。
    /// </summary>
    public abstract class SampleBase : MonoBehaviour
    {
        // LiteRT パイプライン
        protected LiteRtEnvironment Environment;
        protected LiteRtModel Model;
        protected LiteRtOptions Options;
        protected LiteRtCompiledModel CompiledModel;

        // 設定
        [SerializeField] protected string modelFileName;
        protected string ModelPath => Path.Combine(Application.streamingAssetsPath, "LiteRT", "Models", modelFileName);

        // HW アクセラレータ設定
        protected LiteRtHwAccelerators selectedAccelerator = LiteRtHwAccelerators.Cpu;
        protected int cpuThreadCount = 4;
        private int _acceleratorIndex;
        private static readonly string[] AcceleratorNames = { "CPU", "GPU", "CPU + GPU" };

        // 推論時間計測
        protected readonly Stopwatch InferenceStopwatch = new Stopwatch();
        protected double LastInferenceMs;

        // 診断ログ
        private readonly List<string> _logMessages = new List<string>();
        private Vector2 _logScrollPos;
        private const int MaxLogLines = 100;

        // GUI 状態
        protected bool IsModelLoaded;
        protected string ErrorMessage;
        private bool _dllAvailable;

        protected virtual void Start()
        {
            _dllAvailable = CheckDllAvailable();
            if (!_dllAvailable)
            {
                ErrorMessage = "libLiteRt ネイティブライブラリが見つかりません。ビルドスクリプトで配置してください。";
                return;
            }

            // GPU 対応デバイスならデフォルトを GPU にする
            if (SystemInfo.supportsComputeShaders)
            {
                selectedAccelerator = LiteRtHwAccelerators.Gpu;
                _acceleratorIndex = 1; // GPU
            }

            if (!string.IsNullOrEmpty(modelFileName) && File.Exists(ModelPath))
            {
                LoadModel();
            }
        }

        /// <summary>
        /// DLL の存在を確認する。
        /// </summary>
        private static bool CheckDllAvailable()
        {
            try
            {
                // 最も軽量な P/Invoke 呼び出しで DLL ロードを確認
                var env = new LiteRtEnvironment();
                env.Dispose();
                return true;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
            catch (Exception)
            {
                // DLL は存在するが別のエラー → DLL 自体は利用可能
                return true;
            }
        }

        /// <summary>
        /// モデルをロードして LiteRT パイプラインを構築する。
        /// </summary>
        protected void LoadModel()
        {
            DisposeResources();
            ErrorMessage = null;

            try
            {
                Environment = new LiteRtEnvironment();
                Model = LiteRtModel.FromFile(ModelPath);
                Options = CreateOptions();

                try
                {
                    CompiledModel = new LiteRtCompiledModel(Environment, Model, Options);
                }
                catch (LiteRtException) when ((selectedAccelerator & LiteRtHwAccelerators.Gpu) != 0)
                {
                    // GPU コンパイル失敗 → CPU フォールバック
                    Log("GPU コンパイル失敗。CPU にフォールバックします。");
                    selectedAccelerator = LiteRtHwAccelerators.Cpu;
                    _acceleratorIndex = 0;
                    Options.Dispose();
                    Options = CreateOptions();
                    CompiledModel = new LiteRtCompiledModel(Environment, Model, Options);
                }

                IsModelLoaded = true;

                Log($"モデル読み込み完了: {modelFileName}");
                Log($"シグネチャ数: {Model.GetNumSignatures()}");
                Log($"入力テンソル数: {Model.GetNumInputs()}");
                Log($"出力テンソル数: {Model.GetNumOutputs()}");
                OnModelLoaded();
            }
            catch (Exception e)
            {
                ErrorMessage = $"モデル読み込みエラー: {e.Message}";
                Log(ErrorMessage);
                Debug.LogException(e);
                DisposeResources();
            }
        }

        /// <summary>
        /// モデル読み込み後に呼ばれる。サブクラスでオーバーライドしてバッファ作成等を行う。
        /// </summary>
        protected virtual void OnModelLoaded() { }

        /// <summary>
        /// コンパイルオプションを作成する。サブクラスでオーバーライドしてカスタマイズ可能。
        /// </summary>
        protected virtual LiteRtOptions CreateOptions()
        {
            var options = new LiteRtOptions()
                .SetHardwareAccelerators(selectedAccelerator);

            if ((selectedAccelerator & LiteRtHwAccelerators.Cpu) != 0)
            {
                var cpuOpts = new CpuOptions();
                cpuOpts.SetNumThreads(cpuThreadCount);
                options.AddCpuOptions(cpuOpts);
            }

            if ((selectedAccelerator & LiteRtHwAccelerators.Gpu) != 0)
            {
                var gpuOpts = new GpuOptions();
                options.AddGpuOptions(gpuOpts);
            }

            return options;
        }

        /// <summary>
        /// 入力テンソルバッファを作成する。
        /// </summary>
        protected LiteRtTensorBuffer CreateInputBuffer(int inputIndex = 0, int signatureIndex = 0)
        {
            return LiteRtTensorBuffer.CreateFromRequirements(
                Environment, CompiledModel, Model, inputIndex, true, signatureIndex);
        }

        /// <summary>
        /// 出力テンソルバッファを作成する。
        /// </summary>
        protected LiteRtTensorBuffer CreateOutputBuffer(int outputIndex = 0, int signatureIndex = 0)
        {
            return LiteRtTensorBuffer.CreateFromRequirements(
                Environment, CompiledModel, Model, outputIndex, false, signatureIndex);
        }

        /// <summary>
        /// 推論を実行し、時間を計測する。
        /// </summary>
        protected void RunInference(LiteRtTensorBuffer[] inputs, LiteRtTensorBuffer[] outputs,
            int signatureIndex = 0)
        {
            InferenceStopwatch.Restart();
            CompiledModel.Run(inputs, outputs, signatureIndex);
            InferenceStopwatch.Stop();
            LastInferenceMs = InferenceStopwatch.Elapsed.TotalMilliseconds;
        }

        protected virtual void OnGUI()
        {
            // DLL 未検出バナー
            if (!_dllAvailable)
            {
                DrawErrorBanner("libLiteRt ネイティブライブラリが見つかりません。\nビルドスクリプトで配置してください。");
                return;
            }

            GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, Screen.height - 20));

            // タイトル
            GUILayout.Label(GetSampleTitle(), GUI.skin.GetStyle("Label"));
            GUILayout.Space(5);

            // エラー表示
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                var style = new GUIStyle(GUI.skin.label) { normal = { textColor = Color.red } };
                GUILayout.Label(ErrorMessage, style);
            }

            // モデルパス・設定
            DrawModelSettings();

            // サンプル固有 GUI
            if (IsModelLoaded)
            {
                GUILayout.Space(10);
                DrawSampleGUI();
            }

            // 診断ログ
            GUILayout.Space(10);
            DrawLogArea();

            GUILayout.EndArea();
        }

        /// <summary>
        /// モデル設定 UI を描画する。
        /// </summary>
        private void DrawModelSettings()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("モデル:", GUILayout.Width(50));
            var newFileName = GUILayout.TextField(modelFileName ?? "", GUILayout.Width(300));
            if (newFileName != modelFileName)
            {
                modelFileName = newFileName;
            }

            // HW アクセラレータ選択
            GUILayout.Label("HW:", GUILayout.Width(30));
            var newAccIdx = GUILayout.SelectionGrid(_acceleratorIndex, AcceleratorNames, 3, GUILayout.Width(240));
            if (newAccIdx != _acceleratorIndex)
            {
                _acceleratorIndex = newAccIdx;
                switch (_acceleratorIndex)
                {
                    case 0:
                        selectedAccelerator = LiteRtHwAccelerators.Cpu;
                        break;
                    case 1:
                        selectedAccelerator = LiteRtHwAccelerators.Gpu;
                        break;
                    case 2:
                        selectedAccelerator = LiteRtHwAccelerators.Cpu | LiteRtHwAccelerators.Gpu;
                        break;
                }
            }

            // スレッド数
            GUILayout.Label($"スレッド: {cpuThreadCount}", GUILayout.Width(80));
            cpuThreadCount = (int)GUILayout.HorizontalSlider(cpuThreadCount, 1, 8, GUILayout.Width(80));

            // ロードボタン
            if (GUILayout.Button("読み込み", GUILayout.Width(80)))
            {
                if (File.Exists(ModelPath))
                {
                    LoadModel();
                }
                else
                {
                    ErrorMessage = $"モデルファイルが見つかりません: {ModelPath}";
                }
            }

            GUILayout.EndHorizontal();

            // 推論時間
            if (IsModelLoaded && LastInferenceMs > 0)
            {
                GUILayout.Label($"推論時間: {LastInferenceMs:F2} ms");
            }
        }

        /// <summary>
        /// エラーバナーを描画する。
        /// </summary>
        protected static void DrawErrorBanner(string message)
        {
            var style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.red }
            };
            GUI.Box(new Rect(10, 10, Screen.width - 20, 80), message, style);
        }

        /// <summary>
        /// 診断ログ領域を描画する。
        /// </summary>
        private void DrawLogArea()
        {
            GUILayout.Label("--- ログ ---");
            _logScrollPos = GUILayout.BeginScrollView(_logScrollPos, GUILayout.Height(120));
            foreach (var msg in _logMessages)
            {
                GUILayout.Label(msg);
            }
            GUILayout.EndScrollView();
        }

        /// <summary>
        /// サンプルのタイトルを返す。
        /// </summary>
        protected abstract string GetSampleTitle();

        /// <summary>
        /// サンプル固有の GUI を描画する。
        /// </summary>
        protected abstract void DrawSampleGUI();

        /// <summary>
        /// 診断ログにメッセージを追加する。
        /// </summary>
        protected void Log(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            _logMessages.Add($"[{timestamp}] {message}");
            if (_logMessages.Count > MaxLogLines)
                _logMessages.RemoveAt(0);
            Debug.Log($"[LiteRT Sample] {message}");
        }

        /// <summary>
        /// リソースを逆順で破棄する。
        /// </summary>
        protected virtual void DisposeResources()
        {
            IsModelLoaded = false;
            CompiledModel?.Dispose();
            CompiledModel = null;
            // Options は CompiledModel で使用済みだが、所有権は移譲されていないため Dispose する
            Options?.Dispose();
            Options = null;
            Model?.Dispose();
            Model = null;
            Environment?.Dispose();
            Environment = null;
        }

        protected virtual void OnDestroy()
        {
            DisposeResources();
        }
    }
}
