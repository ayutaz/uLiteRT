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
using System.Text.RegularExpressions;
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

        // FastSpeech2 入力テンソル名（5 入力）
        // [0] input_ids: int32 [1, N] — テキストトークン
        // [1] speaker_ids: int32 [1] — 話者 ID
        // [2] speed_ratios: float32 [1] — 速度比率
        // [3] f0_ratios: float32 [1] — ピッチ比率
        // [4] energy_ratios: float32 [1] — エネルギー比率

        [SerializeField] private int speakerId;
        [SerializeField] private float speedRatio = 1.0f;
        [SerializeField] private float f0Ratio = 1.0f;
        [SerializeField] private float energyRatio = 1.0f;

        // バッファ
        private LiteRtTensorBuffer[] _fsInputBuffers;
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
                catch (LiteRtException ex) when ((selectedAccelerator & LiteRtHwAccelerators.Gpu) != 0)
                {
                    LogGpuDiagnostics(ex, "FastSpeech2: ");
                    _fsOptions.Dispose();
                    _fsOptions = new LiteRtOptions()
                        .SetHardwareAccelerators(LiteRtHwAccelerators.Cpu);
                    var cpuOpts = new CpuOptions();
                    cpuOpts.SetNumThreads(cpuThreadCount);
                    _fsOptions.AddCpuOptions(cpuOpts);
                    // フォールバック用にも Buffer モードを設定
                    var rtOpts = new RuntimeOptions();
                    rtOpts.SetErrorReporterMode(LiteRtErrorReporterMode.Buffer);
                    _fsOptions.AddRuntimeOptions(rtOpts);
                    _fsCompiled = new LiteRtCompiledModel(Environment, _fsModel, _fsOptions);

                    try
                    {
                        var msgs = _fsCompiled.GetErrorMessages();
                        if (!string.IsNullOrEmpty(msgs))
                            Log($"  LiteRT 警告: {msgs}");
                    }
                    catch { /* 無視 */ }
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

            // パラメータ調整
            GUILayout.BeginHorizontal();
            GUILayout.Label("速度:", GUILayout.Width(40));
            speedRatio = GUILayout.HorizontalSlider(speedRatio, 0.5f, 2.0f, GUILayout.Width(150));
            GUILayout.Label($"{speedRatio:F2}", GUILayout.Width(40));
            GUILayout.Label("  ピッチ:", GUILayout.Width(60));
            f0Ratio = GUILayout.HorizontalSlider(f0Ratio, 0.5f, 2.0f, GUILayout.Width(150));
            GUILayout.Label($"{f0Ratio:F2}", GUILayout.Width(40));
            GUILayout.EndHorizontal();

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
                Log($"トークン化: \"{inputText}\" → {tokenIds.Length} トークン: [{string.Join(", ", tokenIds)}]");

                // ---- Step 2: FastSpeech2 — トークン → メルスペクトログラム ----
                InferenceStopwatch.Restart();

                int numInputs = _fsModel.GetNumInputs();
                int numOutputs = _fsModel.GetNumOutputs();

                // input_ids [1, N] をリサイズ（他の入力は固定形状のためリサイズ不要）
                _fsCompiled.ResizeInputTensorNonStrict(0, new[] { 1, tokenIds.Length });

                // 出力レイアウト取得（リサイズ後は updateAllocation: true で形状伝搬が必須）
                var outputLayouts = _fsCompiled.GetOutputTensorLayouts(numOutputs, updateAllocation: true);

                // 入力バッファ作成（古いバッファはまだ Dispose しない — Run() まで保持）
                var oldFsInputBuffers = _fsInputBuffers;
                var oldFsOutputBuffers = _fsOutputBuffers;

                _fsInputBuffers = new LiteRtTensorBuffer[numInputs];
                for (int i = 0; i < numInputs; i++)
                {
                    var inLayout = _fsCompiled.GetInputTensorLayout(i);
                    var inElementType = _fsModel.GetInputTensorType(i).elementType;
                    _fsInputBuffers[i] = LiteRtTensorBuffer.CreateFromRequirements(
                        Environment, _fsCompiled, i, true, inElementType, inLayout);
                }

                // 入力データ書き込み
                WriteInt32Buffer(_fsInputBuffers[0], tokenIds);              // input_ids [1, N]
                WriteInt32Buffer(_fsInputBuffers[1], new[] { speakerId });   // speaker_ids [1]
                WriteFloatBuffer(_fsInputBuffers[2], new[] { speedRatio });  // speed_ratios [1]
                WriteFloatBuffer(_fsInputBuffers[3], new[] { f0Ratio });     // f0_ratios [1]
                WriteFloatBuffer(_fsInputBuffers[4], new[] { energyRatio }); // energy_ratios [1]

                // 出力バッファ作成（過大確保）
                // FastSpeech2 の出力はデータ依存の動的形状（duration prediction）。
                // 推定形状は実際より小さい可能性があるため、入力長から最大サイズを逆算する。
                const int maxFramesPerToken = 50;
                long maxOutputFrames = (long)tokenIds.Length * maxFramesPerToken;

                _fsOutputBuffers = new LiteRtTensorBuffer[numOutputs];
                for (int i = 0; i < numOutputs; i++)
                {
                    var outElementType = _fsModel.GetOutputTensorType(i).elementType;
                    var estDims = outputLayouts[i].GetDimensions();
                    long minSize = EstimateDynamicBufferSize(outElementType, estDims, maxOutputFrames);
                    _fsOutputBuffers[i] = LiteRtTensorBuffer.CreateFromRequirements(
                        Environment, _fsCompiled, i, false, outElementType, outputLayouts[i],
                        minimumBufferSize: minSize);
                }

                // Run（内部: RegisterBuffer → AllocateTensors（新バッファで検証）→ Invoke）
                _fsCompiled.Run(_fsInputBuffers, _fsOutputBuffers);

                // 古いバッファを安全に Dispose（Run() が新しいカスタムアロケーションに切替済み）
                if (oldFsInputBuffers != null)
                    foreach (var buf in oldFsInputBuffers) buf?.Dispose();
                if (oldFsOutputBuffers != null)
                    foreach (var buf in oldFsOutputBuffers) buf?.Dispose();

                // ★ 推論後の実際の出力形状を取得
                var actualOutputLayouts = _fsCompiled.GetOutputTensorLayouts(numOutputs, updateAllocation: false);
                var actualMelDims = actualOutputLayouts[1].GetDimensions();

                // ★ 実際のデータサイズだけ読み取る（ReadFloat() はバッファ全体を返すため手動 Lock/Copy）
                int actualMelFloats = 1;
                foreach (var d in actualMelDims) actualMelFloats *= d;
                float[] melData;
                IntPtr melPtr = _fsOutputBuffers[1].Lock(LiteRtTensorBufferLockMode.Read);
                try
                {
                    // PackedSize はバッファ作成時の推定レイアウトに基づくため、
                    // 動的形状の場合は実出力サイズより小さい可能性がある。
                    // Size（実アロケーションサイズ）を容量チェックに使う。
                    int bufferFloatCapacity = _fsOutputBuffers[1].Size / sizeof(float);
                    int safeCount = Math.Min(actualMelFloats, bufferFloatCapacity);
                    melData = new float[safeCount];
                    Marshal.Copy(melPtr, melData, 0, safeCount);
                }
                finally
                {
                    _fsOutputBuffers[1].Unlock();
                }

                Log($"FastSpeech2 出力: [{string.Join(", ", actualMelDims)}] = {actualMelFloats} floats (読み取り: {melData.Length})");

                // ---- Step 3: MelGAN — メルスペクトログラム → 音声波形 ----

                // ★ 実際の FastSpeech2 出力形状で MelGAN をリサイズ（推定値ではなく）
                var oldMelganInput = _melganInputBuffer;
                var oldMelganOutput = _melganOutputBuffer;

                CompiledModel.ResizeInputTensorNonStrict(0, actualMelDims);

                // MelGAN も入力リサイズ後は updateAllocation: true で形状伝搬
                var melganOutputLayouts = CompiledModel.GetOutputTensorLayouts(
                    Model.GetNumOutputs(), updateAllocation: true);

                var melganInputLayout = CompiledModel.GetInputTensorLayout(0);
                _melganInputBuffer = LiteRtTensorBuffer.CreateFromRequirements(
                    Environment, CompiledModel, 0, true,
                    Model.GetInputTensorType(0).elementType, melganInputLayout);

                // MB-MelGAN: mel_frames → wav_samples (アップサンプリング係数 ≈ 300)
                // 安全のため 512 倍で見積もり
                const int maxSamplesPerMelFrame = 512;
                long melFrames = actualMelDims.Length > 1 ? actualMelDims[1] : actualMelDims[0];
                long minMelganOutputSize = melFrames * maxSamplesPerMelFrame * sizeof(float);
                _melganOutputBuffer = LiteRtTensorBuffer.CreateFromRequirements(
                    Environment, CompiledModel, 0, false,
                    Model.GetOutputTensorType(0).elementType, melganOutputLayouts[0],
                    minimumBufferSize: minMelganOutputSize);

                _melganInputBuffer.WriteFloat(melData);
                CompiledModel.Run(new[] { _melganInputBuffer }, new[] { _melganOutputBuffer });

                // 古いバッファを Dispose
                oldMelganInput?.Dispose();
                oldMelganOutput?.Dispose();

                InferenceStopwatch.Stop();
                LastInferenceMs = InferenceStopwatch.Elapsed.TotalMilliseconds;

                // ★ MelGAN 出力サイズの決定:
                // GetOutputTensorLayouts は推論後に実出力形状を反映しない場合がある。
                // バッファ実アロケーションサイズで安全キャップする。
                var melganPostLayouts = CompiledModel.GetOutputTensorLayouts(
                    Model.GetNumOutputs(), updateAllocation: false);
                var reportedDims = melganPostLayouts[0].GetDimensions();
                int reportedFloats = 1;
                foreach (var d in reportedDims) reportedFloats *= d;

                // レイアウトが実出力を反映していない場合（reportedFloats が極端に小さい）、
                // バッファ実サイズから読み取り可能な最大 float 数を使う
                int bufferAllocatedFloats = _melganOutputBuffer.Size / sizeof(float);
                int wavFloats = reportedFloats > 1 ? reportedFloats : bufferAllocatedFloats;

                IntPtr wavPtr = _melganOutputBuffer.Lock(LiteRtTensorBufferLockMode.Read);
                try
                {
                    int safeCount = Math.Min(wavFloats, bufferAllocatedFloats);
                    _generatedWaveform = new float[safeCount];
                    Marshal.Copy(wavPtr, _generatedWaveform, 0, safeCount);
                }
                finally
                {
                    _melganOutputBuffer.Unlock();
                }

                // 末尾の無音（ゼロパディング）をトリム
                _generatedWaveform = TrimTrailingSilence(_generatedWaveform);

                // MelGAN 生出力を [-1, 1] にピーク正規化（AudioClip.SetData の要件）
                NormalizeWaveform(_generatedWaveform);

                if (_generatedClip != null) Destroy(_generatedClip);
                _generatedClip = AudioClip.Create("TTS_Output", _generatedWaveform.Length, 1, OutputSampleRate, false);
                _generatedClip.SetData(_generatedWaveform, 0);

                Log($"音声合成完了 ({LastInferenceMs:F2} ms) — {_generatedWaveform.Length} サンプル, " +
                    $"{(float)_generatedWaveform.Length / OutputSampleRate:F2} 秒");
            }
            catch (LiteRtException le)
            {
                string nativeErrors = null;
                try { nativeErrors = _fsCompiled?.GetErrorMessages(); } catch { /* 無視 */ }

                ErrorMessage = $"合成エラー: {le.Message}";
                if (!string.IsNullOrEmpty(nativeErrors))
                    ErrorMessage += $"\nネイティブエラー: {nativeErrors}";
                Log(ErrorMessage);
                Debug.LogException(le);

                // エラー回復: FastSpeech2 リソースを再構築
                ReloadFastSpeechModel();
            }
            catch (Exception e)
            {
                ErrorMessage = $"合成エラー: {e.Message}";
                Log(ErrorMessage);
                Debug.LogException(e);

                // エラー回復: FastSpeech2 リソースを再構築
                ReloadFastSpeechModel();
            }
        }

        /// <summary>
        /// FastSpeech2 リソースを安全に破棄して再構築する。
        /// </summary>
        private void ReloadFastSpeechModel()
        {
            try
            {
                if (_fsInputBuffers != null)
                    foreach (var buf in _fsInputBuffers) { try { buf?.Dispose(); } catch { } }
                if (_fsOutputBuffers != null)
                    foreach (var buf in _fsOutputBuffers) { try { buf?.Dispose(); } catch { } }
                _fsInputBuffers = null;
                _fsOutputBuffers = null;

                try { _fsCompiled?.Dispose(); } catch { }
                _fsCompiled = null;
                _isFsLoaded = false;

                LoadFastSpeechModel();
            }
            catch (Exception e)
            {
                Log($"FastSpeech2 再構築失敗: {e.Message}");
                _isFsLoaded = false;
            }
        }

        /// <summary>
        /// テキストをトークン ID 配列に変換する。
        /// LJSpeechProcessor 互換: 各文字を直接 symbol_to_id で変換する（文字レベルトークナイズ）。
        /// </summary>
        private int[] TextToTokenIds(string text)
        {
            var ids = new List<int>();
            text = text.ToLowerInvariant().Trim();
            text = Regex.Replace(text, @"\s+", " ");

            // LJSpeechProcessor 互換: 各文字を直接 symbol_to_id で変換
            foreach (char c in text)
            {
                if (_charMapping.TryGetValue(c.ToString(), out int id))
                    ids.Add(id);
                // マッピングにない文字はスキップ（LJSpeechProcessor と同じ挙動）
            }

            // EOS トークン追加
            if (_charMapping.TryGetValue("eos", out int eosId))
                ids.Add(eosId);

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

        /// <summary>
        /// float 配列をテンソルバッファに書き込む。
        /// </summary>
        private static void WriteFloatBuffer(LiteRtTensorBuffer buffer, float[] data)
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

        /// <summary>
        /// 動的出力テンソルの最大バッファサイズを推定する。
        /// LiteRtTensorBuffer.EstimateDynamicBufferSize に委譲。
        /// </summary>
        private static long EstimateDynamicBufferSize(
            LiteRtElementType elementType, int[] dims, long maxFrames)
        {
            return LiteRtTensorBuffer.EstimateDynamicBufferSize(elementType, dims, maxFrames);
        }

        /// <summary>
        /// 波形データを [-1, 1] 範囲にピーク正規化する。
        /// AudioClip.SetData は [-1.0f, 1.0f] 範囲を要求する。
        /// </summary>
        private static void NormalizeWaveform(float[] waveform)
        {
            float maxAbs = 0f;
            for (int i = 0; i < waveform.Length; i++)
            {
                float abs = Mathf.Abs(waveform[i]);
                if (abs > maxAbs) maxAbs = abs;
            }

            if (maxAbs > 1f)
            {
                float scale = 1f / maxAbs;
                for (int i = 0; i < waveform.Length; i++)
                    waveform[i] *= scale;
            }
        }

        /// <summary>
        /// 末尾の連続する無音（ゼロ）サンプルをトリムする。
        /// 過大確保バッファのゼロパディング除去用。
        /// </summary>
        private static float[] TrimTrailingSilence(float[] waveform)
        {
            int end = waveform.Length;
            while (end > 0 && waveform[end - 1] == 0f)
                end--;

            if (end == waveform.Length)
                return waveform;
            if (end == 0)
                return Array.Empty<float>();

            var trimmed = new float[end];
            Array.Copy(waveform, trimmed, end);
            return trimmed;
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
            if (_fsInputBuffers != null)
            {
                foreach (var buf in _fsInputBuffers) { try { buf?.Dispose(); } catch { } }
                _fsInputBuffers = null;
            }
            if (_fsOutputBuffers != null)
            {
                foreach (var buf in _fsOutputBuffers) { try { buf?.Dispose(); } catch { } }
                _fsOutputBuffers = null;
            }
            try { _melganInputBuffer?.Dispose(); } catch { }
            _melganInputBuffer = null;
            try { _melganOutputBuffer?.Dispose(); } catch { }
            _melganOutputBuffer = null;

            try { _fsCompiled?.Dispose(); } catch { }
            _fsCompiled = null;
            try { _fsOptions?.Dispose(); } catch { }
            _fsOptions = null;
            try { _fsModel?.Dispose(); } catch { }
            _fsModel = null;
            _isFsLoaded = false;

            if (_generatedClip != null) Destroy(_generatedClip);
            _generatedClip = null;

            base.DisposeResources();
        }
    }
}
