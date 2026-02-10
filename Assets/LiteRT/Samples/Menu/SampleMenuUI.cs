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

// サンプルメニュー画面

using UnityEngine;
using UnityEngine.SceneManagement;

namespace LiteRT.Samples
{
    /// <summary>
    /// サンプル一覧メニュー。各サンプルシーンへナビゲーションする。
    /// </summary>
    public class SampleMenuUI : MonoBehaviour
    {
        private struct SampleEntry
        {
            public string title;
            public string description;
            public string sceneName;
        }

        private static readonly SampleEntry[] Samples =
        {
            new SampleEntry
            {
                title = "画像分類 (Image Classification)",
                description = "MobileNet V2 による ImageNet 1001 クラス分類",
                sceneName = "ImageClassification"
            },
            new SampleEntry
            {
                title = "物体検出 (Object Detection)",
                description = "SSD MobileNet V1 による物体検出・バウンディングボックス描画",
                sceneName = "ObjectDetection"
            },
            new SampleEntry
            {
                title = "画像セグメンテーション (Image Segmentation)",
                description = "DeepLab V3 によるピクセル単位の 21 クラス分類",
                sceneName = "ImageSegmentation"
            },
            new SampleEntry
            {
                title = "姿勢推定 (Pose Estimation)",
                description = "PoseNet MobileNet による 17 キーポイント検出",
                sceneName = "PoseEstimation"
            },
            new SampleEntry
            {
                title = "スタイル変換 (Style Transfer)",
                description = "Magenta による任意画像のスタイル変換",
                sceneName = "StyleTransfer"
            },
            new SampleEntry
            {
                title = "音声分類 (Sound Classification)",
                description = "YAMNet によるマイク入力の 521 クラス音声分類",
                sceneName = "SoundClassification"
            },
            new SampleEntry
            {
                title = "テキスト音声合成 (Text to Speech)",
                description = "FastSpeech2 + MB-MelGAN による英語 TTS",
                sceneName = "TextToSpeech"
            },
        };

        private Vector2 _scrollPos;

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, Screen.height - 20));

            GUILayout.Label("uLiteRT サンプル一覧", GUI.skin.GetStyle("Label"));
            GUILayout.Space(10);

            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            foreach (var sample in Samples)
            {
                GUILayout.BeginVertical(GUI.skin.box);

                GUILayout.BeginHorizontal();
                GUILayout.Label(sample.title, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("開く", GUILayout.Width(80)))
                {
                    SceneManager.LoadScene(sample.sceneName);
                }
                GUILayout.EndHorizontal();

                var descStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    normal = { textColor = Color.gray }
                };
                GUILayout.Label(sample.description, descStyle);

                GUILayout.EndVertical();
                GUILayout.Space(4);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
