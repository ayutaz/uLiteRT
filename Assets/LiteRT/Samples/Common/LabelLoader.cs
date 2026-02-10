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

// ラベルファイル読み込みヘルパー

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LiteRT.Samples
{
    /// <summary>
    /// ラベルファイルの読み込みユーティリティ。
    /// </summary>
    public static class LabelLoader
    {
        /// <summary>
        /// テキストファイルからラベルを読み込む（1行1ラベル）。
        /// ImageNet ラベルや COCO ラベルで使用。
        /// </summary>
        /// <param name="fileName">Labels/ 配下のファイル名。</param>
        /// <returns>ラベル配列。</returns>
        public static string[] LoadLabels(string fileName)
        {
            var path = Path.Combine(Application.streamingAssetsPath, "LiteRT", "Labels", fileName);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"ラベルファイルが見つかりません: {path}");
                return new string[0];
            }
            return File.ReadAllLines(path);
        }

        /// <summary>
        /// YAMNet 用の CSV ラベルを読み込む。
        /// CSV 形式: index,mid,display_name
        /// </summary>
        /// <param name="fileName">Labels/ 配下の CSV ファイル名。</param>
        /// <returns>インデックス → 表示名の辞書。</returns>
        public static Dictionary<int, string> LoadYamNetLabels(string fileName)
        {
            var labels = new Dictionary<int, string>();
            var path = Path.Combine(Application.streamingAssetsPath, "LiteRT", "Labels", fileName);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"ラベルファイルが見つかりません: {path}");
                return labels;
            }

            var lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                // ヘッダー行スキップ
                if (i == 0 && line.StartsWith("index")) continue;

                var parts = line.Split(',');
                if (parts.Length >= 3 && int.TryParse(parts[0], out int index))
                {
                    labels[index] = parts[2].Trim('"', ' ');
                }
            }

            return labels;
        }

        /// <summary>
        /// JSON ファイルから文字→ID マッピングを読み込む（TTS 用）。
        /// </summary>
        /// <param name="fileName">TTS/ 配下の JSON ファイル名。</param>
        /// <returns>文字 → ID のマッピング辞書。</returns>
        public static Dictionary<string, int> LoadCharacterMapping(string fileName)
        {
            var mapping = new Dictionary<string, int>();
            var path = Path.Combine(Application.streamingAssetsPath, "LiteRT", "TTS", fileName);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"マッピングファイルが見つかりません: {path}");
                return mapping;
            }

            // JsonUtility は Dictionary をサポートしないため、簡易パーサーで処理
            // "symbol_to_id" セクションの {...} を抽出（ネスト安全）
            var json = File.ReadAllText(path);

            int anchor = json.IndexOf("\"symbol_to_id\"", System.StringComparison.Ordinal);
            if (anchor < 0) return mapping;

            int braceStart = json.IndexOf('{', anchor);
            if (braceStart < 0) return mapping;

            int depth = 1;
            int pos = braceStart + 1;
            while (pos < json.Length && depth > 0)
            {
                if (json[pos] == '{') depth++;
                else if (json[pos] == '}') depth--;
                pos++;
            }

            // braceStart+1 ～ pos-2 が symbol_to_id の中身（フラットな key:value 列）
            string section = json.Substring(braceStart + 1, pos - braceStart - 2);
            var entries = section.Split(',');
            foreach (var entry in entries)
            {
                int lastColon = entry.LastIndexOf(':');
                if (lastColon > 0)
                {
                    var key = entry.Substring(0, lastColon).Trim().Trim('"');
                    var valStr = entry.Substring(lastColon + 1).Trim();
                    if (int.TryParse(valStr, out int value))
                    {
                        mapping[key] = value;
                    }
                }
            }

            return mapping;
        }

        /// <summary>
        /// 確率配列から Top-K のインデックスと値を取得する。
        /// </summary>
        /// <param name="probabilities">確率配列。</param>
        /// <param name="k">取得する上位数。</param>
        /// <returns>(インデックス, 確率) のペア配列。</returns>
        public static (int index, float probability)[] GetTopK(float[] probabilities, int k)
        {
            if (probabilities == null || probabilities.Length == 0)
                return new (int, float)[0];

            k = Mathf.Min(k, probabilities.Length);
            var indices = new int[probabilities.Length];
            for (int i = 0; i < indices.Length; i++) indices[i] = i;

            // 部分ソート（Top-K のみ確保）
            for (int i = 0; i < k; i++)
            {
                int maxIdx = i;
                for (int j = i + 1; j < probabilities.Length; j++)
                {
                    if (probabilities[indices[j]] > probabilities[indices[maxIdx]])
                        maxIdx = j;
                }
                // swap
                (indices[i], indices[maxIdx]) = (indices[maxIdx], indices[i]);
            }

            var result = new (int, float)[k];
            for (int i = 0; i < k; i++)
            {
                result[i] = (indices[i], probabilities[indices[i]]);
            }
            return result;
        }
    }
}
