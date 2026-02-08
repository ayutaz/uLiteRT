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

// 音声分類サンプル — YAMNet

using System;
using System.Collections.Generic;
using UnityEngine;

namespace LiteRT.Samples
{
    /// <summary>
    /// YAMNet による音声分類サンプル。
    /// 入力: float32 [1, 15600] — 16kHz モノラル波形（0.975秒）
    /// 出力: float32 [1, 521] — AudioSet 521 クラス確率
    /// </summary>
    public class SoundClassificationSample : SampleBase
    {
        private const int SampleRate = 16000;
        private const int InputSamples = 15600; // 0.975秒
        private const string LabelFileName = "yamnet_class_map.csv";

        private Dictionary<int, string> _labels;
        private LiteRtTensorBuffer _inputBuffer;
        private LiteRtTensorBuffer _outputBuffer;

        private (int index, float probability)[] _topResults;

        // マイク関連
        private AudioClip _micClip;
        private bool _isRecording;
        private string _selectedDevice;
        private int _deviceIndex;
        private string[] _devices;
        private bool _realtimeMode;

        // 波形プレビュー
        private float[] _lastWaveform;

        protected override void Start()
        {
            modelFileName = "yamnet.tflite";
            base.Start();
            _labels = LabelLoader.LoadYamNetLabels(LabelFileName);
            if (_labels.Count > 0)
                Log($"ラベル読み込み完了: {_labels.Count} クラス");

            // マイクデバイス列挙
            _devices = Microphone.devices;
            if (_devices.Length > 0)
            {
                _selectedDevice = _devices[0];
                Log($"マイクデバイス検出: {_devices.Length} 台");
            }
            else
            {
                Log("マイクデバイスが見つかりません");
            }
        }

        protected override string GetSampleTitle()
        {
            return "音声分類 — YAMNet";
        }

        protected override void OnModelLoaded()
        {
            _inputBuffer?.Dispose();
            _outputBuffer?.Dispose();
            _inputBuffer = CreateInputBuffer();
            _outputBuffer = CreateOutputBuffer();
        }

        private void Update()
        {
            if (_realtimeMode && _isRecording && IsModelLoaded)
            {
                RunRealtimeClassification();
            }
        }

        protected override void DrawSampleGUI()
        {
            // マイク選択
            if (_devices != null && _devices.Length > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("マイク:", GUILayout.Width(50));
                var newIdx = GUILayout.SelectionGrid(_deviceIndex, _devices, _devices.Length, GUILayout.Width(400));
                if (newIdx != _deviceIndex)
                {
                    _deviceIndex = newIdx;
                    _selectedDevice = _devices[_deviceIndex];
                    if (_isRecording) StopRecording();
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("マイクデバイスが見つかりません");
                return;
            }

            GUILayout.Space(5);

            // 録音コントロール
            GUILayout.BeginHorizontal();

            if (!_isRecording)
            {
                if (GUILayout.Button("録音開始", GUILayout.Width(120), GUILayout.Height(35)))
                {
                    StartRecording();
                }
            }
            else
            {
                if (GUILayout.Button("録音停止", GUILayout.Width(120), GUILayout.Height(35)))
                {
                    StopRecording();
                }
            }

            if (_isRecording && !_realtimeMode)
            {
                if (GUILayout.Button("分類実行", GUILayout.Width(120), GUILayout.Height(35)))
                {
                    RunClassificationFromMic();
                }
            }

            // リアルタイムモード
            var newRealtime = GUILayout.Toggle(_realtimeMode, "リアルタイム", GUILayout.Width(100));
            if (newRealtime != _realtimeMode)
            {
                _realtimeMode = newRealtime;
                if (_realtimeMode && !_isRecording) StartRecording();
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // 波形プレビュー
            if (_lastWaveform != null)
            {
                GUILayout.Label("波形:");
                var waveRect = GUILayoutUtility.GetRect(500, 60);
                DrawWaveform(waveRect, _lastWaveform);
            }

            // 分類結果
            if (_topResults != null && _topResults.Length > 0)
            {
                GUILayout.Space(10);
                GUILayout.Label("--- Top-5 分類結果 ---");
                foreach (var (index, probability) in _topResults)
                {
                    string label = _labels.TryGetValue(index, out var name) ? name : $"クラス {index}";
                    GUILayout.Label($"  {label}: {probability:P2}");
                }
            }
        }

        private void StartRecording()
        {
            if (string.IsNullOrEmpty(_selectedDevice)) return;
            // ループ録音（最大10秒、16kHz、モノラル）
            _micClip = Microphone.Start(_selectedDevice, true, 10, SampleRate);
            _isRecording = true;
            Log("録音開始");
        }

        private void StopRecording()
        {
            if (!_isRecording) return;
            Microphone.End(_selectedDevice);
            _isRecording = false;
            Log("録音停止");
        }

        private void RunClassificationFromMic()
        {
            if (_micClip == null) return;

            // マイクの現在位置から 0.975秒分のサンプルを取得
            var samples = GetMicSamples();
            if (samples == null) return;

            RunClassification(samples);
        }

        private void RunRealtimeClassification()
        {
            if (_micClip == null) return;

            var samples = GetMicSamples();
            if (samples == null) return;

            RunClassification(samples);
        }

        private float[] GetMicSamples()
        {
            int micPos = Microphone.GetPosition(_selectedDevice);
            if (micPos < InputSamples) return null;

            var allSamples = new float[_micClip.samples * _micClip.channels];
            _micClip.GetData(allSamples, 0);

            // 最新の InputSamples サンプルを取得
            var samples = new float[InputSamples];
            int startPos = micPos - InputSamples;
            if (startPos < 0) startPos += allSamples.Length;
            Array.Copy(allSamples, startPos, samples, 0, InputSamples);

            _lastWaveform = samples;
            return samples;
        }

        private void RunClassification(float[] samples)
        {
            try
            {
                ErrorMessage = null;

                _inputBuffer.WriteFloat(samples);
                RunInference(
                    new[] { _inputBuffer },
                    new[] { _outputBuffer });

                var output = _outputBuffer.ReadFloat();
                _topResults = LabelLoader.GetTopK(output, 5);

                if (!_realtimeMode)
                {
                    Log($"分類完了 ({LastInferenceMs:F2} ms) — Top-1: " +
                        (_topResults.Length > 0 && _labels.TryGetValue(_topResults[0].index, out var name)
                            ? $"{name} ({_topResults[0].probability:P2})"
                            : "不明"));
                }
            }
            catch (Exception e)
            {
                ErrorMessage = $"推論エラー: {e.Message}";
                if (!_realtimeMode) Log(ErrorMessage);
            }
        }

        private static void DrawWaveform(Rect rect, float[] waveform)
        {
            var prevColor = GUI.color;
            GUI.color = new Color(0.2f, 0.2f, 0.2f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.green;

            int step = Mathf.Max(1, waveform.Length / (int)rect.width);
            float centerY = rect.y + rect.height / 2f;

            for (int i = 0; i < (int)rect.width; i++)
            {
                int sampleIdx = i * step;
                if (sampleIdx >= waveform.Length) break;

                float sample = waveform[sampleIdx];
                float barHeight = Mathf.Abs(sample) * rect.height / 2f;

                GUI.DrawTexture(new Rect(
                    rect.x + i, centerY - barHeight,
                    1, barHeight * 2), Texture2D.whiteTexture);
            }

            GUI.color = prevColor;
        }

        protected override void DisposeResources()
        {
            if (_isRecording) StopRecording();
            _inputBuffer?.Dispose();
            _inputBuffer = null;
            _outputBuffer?.Dispose();
            _outputBuffer = null;
            base.DisposeResources();
        }
    }
}
