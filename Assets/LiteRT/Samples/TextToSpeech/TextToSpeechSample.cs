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

// TTS サンプル — FastSpeech2 + MB-MelGAN

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LiteRT.Samples
{
    /// <summary>
    /// FastSpeech2 + MB-MelGAN によるテキスト音声合成サンプル。
    /// 2段階: FastSpeech2 (テキスト → メルスペクトログラム) → MelGAN (メル → 音声波形)
    /// 英語のみ対応。
    /// </summary>
    public class TextToSpeechSample : SampleBase
    {
        private const string FastSpeechModelFileName = "fastspeech2.tflite";
        private const string MelGanModelFileName = "mb_melgan.tflite";
        private const string MapperFileName = "ljspeech_mapper.json";
        private const int OutputSampleRate = 22050;

        [SerializeField] private string inputText = "Hello, this is a test.";

        // FastSpeech2 モデル
        private LiteRtModel _fsModel;
        private LiteRtOptions _fsOptions;
        private LiteRtCompiledModel _fsCompiled;

        // MelGAN モデル（メインモデルとして使用）
        // base.Model = MelGAN

        // バッファ
        private LiteRtTensorBuffer _fsInputBuffer;
        private LiteRtTensorBuffer[] _fsOutputBuffers;
        private LiteRtTensorBuffer _melganInputBuffer;
        private LiteRtTensorBuffer _melganOutputBuffer;

        private Dictionary<string, int> _charMapping;
        private AudioClip _generatedClip;
        private AudioSource _audioSource;
        private float[] _generatedWaveform;
        private bool _isFsLoaded;

        protected override void Start()
        {
            // MelGAN をメインモデルとしてロード
            modelFileName = MelGanModelFileName;
            base.Start();

            _charMapping = LabelLoader.LoadCharacterMapping(MapperFileName);
            if (_charMapping.Count > 0)
                Log($"文字マッピング読み込み完了: {_charMapping.Count} 文字");

            // AudioSource 取得/追加
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
                _audioSource = gameObject.AddComponent<AudioSource>();
        }

        protected override string GetSampleTitle()
        {
            return "テキスト音声合成 — FastSpeech2 + MB-MelGAN";
        }

        protected override void OnModelLoaded()
        {
            // MelGAN バッファ
            _melganInputBuffer?.Dispose();
            _melganOutputBuffer?.Dispose();

            // MelGAN の入出力バッファはモデル読み込み後に作成
            // ただし入力サイズが動的なため、推論時に作成する

            // FastSpeech2 モデルも読み込む
            LoadFastSpeechModel();
        }

        private void LoadFastSpeechModel()
        {
            try
            {
                var fsPath = System.IO.Path.Combine(
                    Application.streamingAssetsPath, "LiteRT", "Models", FastSpeechModelFileName);

                _fsModel = LiteRtModel.FromFile(fsPath);
                _fsOptions = new LiteRtOptions()
                    .SetHardwareAccelerators(selectedAccelerator);

                if ((selectedAccelerator & LiteRtHwAccelerators.Cpu) != 0)
                {
                    var cpuOpts = new CpuOptions();
                    cpuOpts.SetNumThreads(cpuThreadCount);
                    _fsOptions.AddCpuOptions(cpuOpts);
                }

                if ((selectedAccelerator & LiteRtHwAccelerators.Gpu) != 0)
                {
                    var gpuOpts = new GpuOptions();
                    _fsOptions.AddGpuOptions(gpuOpts);
                }

                try
                {
                    _fsCompiled = new LiteRtCompiledModel(Environment, _fsModel, _fsOptions);
                }
                catch (LiteRtException) when ((selectedAccelerator & LiteRtHwAccelerators.Gpu) != 0)
                {
                    Log("FastSpeech2: GPU コンパイル失敗。CPU にフォールバックします。");
                    _fsOptions.Dispose();
                    _fsOptions = new LiteRtOptions()
                        .SetHardwareAccelerators(LiteRtHwAccelerators.Cpu);
                    var cpuOpts = new CpuOptions();
                    cpuOpts.SetNumThreads(cpuThreadCount);
                    _fsOptions.AddCpuOptions(cpuOpts);
                    _fsCompiled = new LiteRtCompiledModel(Environment, _fsModel, _fsOptions);
                }

                _isFsLoaded = true;

                int numOutputs = _fsModel.GetNumOutputs();
                Log($"FastSpeech2 読み込み完了 (出力: {numOutputs} テンソル)");
            }
            catch (Exception e)
            {
                Log($"FastSpeech2 読み込みエラー: {e.Message}");
            }
        }

        protected override void DrawSampleGUI()
        {
            // テキスト入力
            GUILayout.Label("入力テキスト (英語):");
            inputText = GUILayout.TextField(inputText, GUILayout.Width(500));

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();

            var canRun = _isFsLoaded && IsModelLoaded && _charMapping.Count > 0;
            GUI.enabled = canRun;
            if (GUILayout.Button("音声合成", GUILayout.Width(120), GUILayout.Height(40)))
            {
                RunTTS();
            }
            GUI.enabled = true;

            if (_generatedClip != null)
            {
                if (GUILayout.Button("再生", GUILayout.Width(80), GUILayout.Height(40)))
                {
                    _audioSource.clip = _generatedClip;
                    _audioSource.Play();
                }
                if (GUILayout.Button("停止", GUILayout.Width(80), GUILayout.Height(40)))
                {
                    _audioSource.Stop();
                }
            }

            if (!canRun)
            {
                GUILayout.Label("  モデルとマッピングファイルを確認してください");
            }

            GUILayout.EndHorizontal();

            // 波形プレビュー
            if (_generatedWaveform != null)
            {
                GUILayout.Space(10);
                GUILayout.Label($"生成波形 ({_generatedWaveform.Length} サンプル, {(float)_generatedWaveform.Length / OutputSampleRate:F2} 秒):");
                var waveRect = GUILayoutUtility.GetRect(500, 80);
                DrawWaveform(waveRect);
            }
        }

        private void RunTTS()
        {
            if (string.IsNullOrEmpty(inputText))
            {
                ErrorMessage = "テキストを入力してください。";
                return;
            }

            try
            {
                ErrorMessage = null;

                // Step 1: テキスト → トークン ID
                var tokenIds = TextToTokenIds(inputText);
                Log($"トークン化: {inputText.Length} 文字 → {tokenIds.Length} トークン");

                // Step 2: FastSpeech2 — トークン → メルスペクトログラム
                InferenceStopwatch.Restart();

                // FastSpeech2 入力バッファ作成（動的サイズ）
                _fsInputBuffer?.Dispose();

                // 入力テンソルをリサイズ
                _fsCompiled.ResizeInputTensorNonStrict(0, new[] { 1, tokenIds.Length });

                _fsInputBuffer = LiteRtTensorBuffer.CreateFromRequirements(
                    Environment, _fsCompiled, _fsModel, 0, true);

                // int32 配列をバッファに書き込み
                WriteInt32Buffer(_fsInputBuffer, tokenIds);

                // FastSpeech2 出力バッファ作成
                if (_fsOutputBuffers != null)
                    foreach (var buf in _fsOutputBuffers) buf?.Dispose();

                int numOutputs = _fsModel.GetNumOutputs();
                _fsOutputBuffers = new LiteRtTensorBuffer[numOutputs];
                for (int i = 0; i < numOutputs; i++)
                {
                    _fsOutputBuffers[i] = LiteRtTensorBuffer.CreateFromRequirements(
                        Environment, _fsCompiled, _fsModel, i, false);
                }

                _fsCompiled.Run(
                    new[] { _fsInputBuffer },
                    _fsOutputBuffers);

                // メルスペクトログラムを取得（最初の出力テンソル）
                var melData = _fsOutputBuffers[0].ReadFloat();

                // Step 3: MelGAN — メルスペクトログラム → 音声波形
                _melganInputBuffer?.Dispose();
                _melganOutputBuffer?.Dispose();

                _melganInputBuffer = LiteRtTensorBuffer.CreateFromRequirements(
                    Environment, CompiledModel, Model, 0, true);
                _melganOutputBuffer = LiteRtTensorBuffer.CreateFromRequirements(
                    Environment, CompiledModel, Model, 0, false);

                _melganInputBuffer.WriteFloat(melData);
                CompiledModel.Run(
                    new[] { _melganInputBuffer },
                    new[] { _melganOutputBuffer });

                InferenceStopwatch.Stop();
                LastInferenceMs = InferenceStopwatch.Elapsed.TotalMilliseconds;

                // 波形取得 → AudioClip 変換
                _generatedWaveform = _melganOutputBuffer.ReadFloat();

                if (_generatedClip != null) Destroy(_generatedClip);
                _generatedClip = AudioClip.Create("TTS_Output", _generatedWaveform.Length, 1, OutputSampleRate, false);
                _generatedClip.SetData(_generatedWaveform, 0);

                Log($"音声合成完了 ({LastInferenceMs:F2} ms) — {_generatedWaveform.Length} サンプル, " +
                    $"{(float)_generatedWaveform.Length / OutputSampleRate:F2} 秒");
            }
            catch (Exception e)
            {
                ErrorMessage = $"合成エラー: {e.Message}";
                Log(ErrorMessage);
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// テキストをトークン ID 配列に変換する。
        /// </summary>
        private int[] TextToTokenIds(string text)
        {
            var ids = new List<int>();
            text = text.ToLowerInvariant();

            foreach (char c in text)
            {
                string key = c.ToString();
                if (_charMapping.TryGetValue(key, out int id))
                {
                    ids.Add(id);
                }
                // マッピングにない文字はスキップ
            }

            return ids.ToArray();
        }

        /// <summary>
        /// int32 配列をテンソルバッファに書き込む。
        /// </summary>
        private static void WriteInt32Buffer(LiteRtTensorBuffer buffer, int[] data)
        {
            var ptr = buffer.Lock(LiteRtTensorBufferLockMode.Write);
            try
            {
                Marshal.Copy(data, 0, ptr, data.Length);
            }
            finally
            {
                buffer.Unlock();
            }
        }

        private void DrawWaveform(Rect rect)
        {
            if (_generatedWaveform == null) return;

            var prevColor = GUI.color;
            GUI.color = new Color(0.15f, 0.15f, 0.15f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(0.3f, 0.8f, 1.0f);

            int step = Mathf.Max(1, _generatedWaveform.Length / (int)rect.width);
            float centerY = rect.y + rect.height / 2f;

            for (int i = 0; i < (int)rect.width; i++)
            {
                int sampleIdx = i * step;
                if (sampleIdx >= _generatedWaveform.Length) break;

                float sample = _generatedWaveform[sampleIdx];
                float barHeight = Mathf.Abs(sample) * rect.height / 2f;

                GUI.DrawTexture(new Rect(
                    rect.x + i, centerY - barHeight,
                    1, barHeight * 2), Texture2D.whiteTexture);
            }

            GUI.color = prevColor;
        }

        protected override void DisposeResources()
        {
            _fsInputBuffer?.Dispose();
            _fsInputBuffer = null;
            if (_fsOutputBuffers != null)
            {
                foreach (var buf in _fsOutputBuffers) buf?.Dispose();
                _fsOutputBuffers = null;
            }
            _melganInputBuffer?.Dispose();
            _melganInputBuffer = null;
            _melganOutputBuffer?.Dispose();
            _melganOutputBuffer = null;

            _fsCompiled?.Dispose();
            _fsCompiled = null;
            _fsOptions?.Dispose();
            _fsOptions = null;
            _fsModel?.Dispose();
            _fsModel = null;
            _isFsLoaded = false;

            if (_generatedClip != null) Destroy(_generatedClip);
            _generatedClip = null;

            base.DisposeResources();
        }
    }
}
