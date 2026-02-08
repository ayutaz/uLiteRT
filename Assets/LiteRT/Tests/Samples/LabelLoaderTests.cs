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

// LabelLoader ユニットテスト

using LiteRT.Samples;
using NUnit.Framework;

namespace LiteRT.Tests.Samples
{
    [TestFixture]
    public class LabelLoaderTests
    {
        [Test]
        public void GetTopK_正常な配列からTop3を取得()
        {
            var probs = new float[] { 0.1f, 0.5f, 0.3f, 0.8f, 0.2f };
            var result = LabelLoader.GetTopK(probs, 3);

            Assert.AreEqual(3, result.Length);
            // Top-1 はインデックス 3 (0.8)
            Assert.AreEqual(3, result[0].index);
            Assert.AreEqual(0.8f, result[0].probability, 0.001f);
            // Top-2 はインデックス 1 (0.5)
            Assert.AreEqual(1, result[1].index);
            Assert.AreEqual(0.5f, result[1].probability, 0.001f);
            // Top-3 はインデックス 2 (0.3)
            Assert.AreEqual(2, result[2].index);
            Assert.AreEqual(0.3f, result[2].probability, 0.001f);
        }

        [Test]
        public void GetTopK_Kが配列長を超える場合は配列長に制限()
        {
            var probs = new float[] { 0.1f, 0.2f };
            var result = LabelLoader.GetTopK(probs, 10);

            Assert.AreEqual(2, result.Length);
        }

        [Test]
        public void GetTopK_空配列は空結果()
        {
            var result = LabelLoader.GetTopK(new float[0], 5);
            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void GetTopK_nullは空結果()
        {
            var result = LabelLoader.GetTopK(null, 5);
            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void GetTopK_全て同じ値の場合も動作する()
        {
            var probs = new float[] { 0.5f, 0.5f, 0.5f };
            var result = LabelLoader.GetTopK(probs, 2);
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(0.5f, result[0].probability, 0.001f);
            Assert.AreEqual(0.5f, result[1].probability, 0.001f);
        }

        [Test]
        public void GetTopK_単一要素()
        {
            var probs = new float[] { 0.9f };
            var result = LabelLoader.GetTopK(probs, 1);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(0, result[0].index);
            Assert.AreEqual(0.9f, result[0].probability, 0.001f);
        }

        [Test]
        public void GetTopK_負の値を含む場合も正しくソート()
        {
            var probs = new float[] { -0.5f, 0.1f, -0.1f, 0.3f };
            var result = LabelLoader.GetTopK(probs, 2);
            Assert.AreEqual(3, result[0].index, "最大はインデックス 3");
            Assert.AreEqual(1, result[1].index, "次はインデックス 1");
        }

        [Test]
        public void LoadLabels_存在しないファイルは空配列()
        {
            var result = LabelLoader.LoadLabels("nonexistent_file_xyz.txt");
            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void LoadYamNetLabels_存在しないファイルは空辞書()
        {
            var result = LabelLoader.LoadYamNetLabels("nonexistent_file_xyz.csv");
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void LoadCharacterMapping_存在しないファイルは空辞書()
        {
            var result = LabelLoader.LoadCharacterMapping("nonexistent_file_xyz.json");
            Assert.AreEqual(0, result.Count);
        }
    }
}
