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

// ImageHelper ユニットテスト

using LiteRT.Samples;
using NUnit.Framework;
using UnityEngine;

namespace LiteRT.Tests.Samples
{
    [TestFixture]
    public class ImageHelperTests
    {
        private Texture2D _testTexture;

        [SetUp]
        public void SetUp()
        {
            // 4x4 のテストテクスチャを作成（赤、緑、青、白のパターン）
            _testTexture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color32[16];
            // 左上から右下へ: 赤(0,0) 緑(1,0) 青(2,0) 白(3,0) ...
            pixels[12] = new Color32(255, 0, 0, 255);   // (0,3) → 左上 (Unity座標系では左下=0)
            pixels[13] = new Color32(0, 255, 0, 255);   // (1,3)
            pixels[14] = new Color32(0, 0, 255, 255);   // (2,3)
            pixels[15] = new Color32(255, 255, 255, 255); // (3,3)
            // 他は黒
            for (int i = 0; i < 12; i++) pixels[i] = new Color32(0, 0, 0, 255);
            _testTexture.SetPixels32(pixels);
            _testTexture.Apply();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testTexture != null) Object.DestroyImmediate(_testTexture);
        }

        [Test]
        public void TextureToFloatArray_出力サイズが正しい()
        {
            var result = ImageHelper.TextureToFloatArray(_testTexture);
            // 4x4x3 = 48
            Assert.AreEqual(48, result.Length);
        }

        [Test]
        public void TextureToFloatArray_正規化なしで0から1の範囲()
        {
            var result = ImageHelper.TextureToFloatArray(_testTexture);
            foreach (var val in result)
            {
                Assert.GreaterOrEqual(val, 0f);
                Assert.LessOrEqual(val, 1f);
            }
        }

        [Test]
        public void TextureToFloatArray_正規化ありで値が変換される()
        {
            // mean=0.5, std=0.5 で [0,1] → [-1,1] に変換
            var result = ImageHelper.TextureToFloatArray(_testTexture, 0.5f, 0.5f);
            foreach (var val in result)
            {
                Assert.GreaterOrEqual(val, -1f, "値が -1 未満");
                Assert.LessOrEqual(val, 1f, "値が 1 超過");
            }
        }

        [Test]
        public void TextureToByteArray_出力サイズが正しい()
        {
            var result = ImageHelper.TextureToByteArray(_testTexture);
            Assert.AreEqual(48, result.Length);
        }

        [Test]
        public void TextureToByteArray_左上ピクセルが赤()
        {
            var result = ImageHelper.TextureToByteArray(_testTexture);
            // NHWC形式: (y=0, x=0) のRGB → index 0,1,2
            // y=0 は画像の上端 (Unity座標系の y=3)
            Assert.AreEqual(255, result[0], "R チャネル");
            Assert.AreEqual(0, result[1], "G チャネル");
            Assert.AreEqual(0, result[2], "B チャネル");
        }

        [Test]
        public void FloatArrayToTexture_出力サイズが正しい()
        {
            // 2x2 の赤画像
            var data = new float[]
            {
                1f, 0f, 0f, 1f, 0f, 0f,
                1f, 0f, 0f, 1f, 0f, 0f,
            };
            var tex = ImageHelper.FloatArrayToTexture(data, 2, 2);
            Assert.AreEqual(2, tex.width);
            Assert.AreEqual(2, tex.height);
            Object.DestroyImmediate(tex);
        }

        [Test]
        public void FloatArrayToTexture_値がクランプされる()
        {
            // 範囲外の値を含むデータ
            var data = new float[] { 2f, -1f, 0.5f };
            var tex = ImageHelper.FloatArrayToTexture(data, 1, 1);
            var pixel = tex.GetPixels32()[0];
            Assert.AreEqual(255, pixel.r, "2.0 は 255 にクランプ");
            Assert.AreEqual(0, pixel.g, "-1.0 は 0 にクランプ");
            Assert.AreEqual(128, pixel.b, 1, "0.5 は約 128");
            Object.DestroyImmediate(tex);
        }

        [Test]
        public void Resize_出力サイズが正しい()
        {
            var resized = ImageHelper.Resize(_testTexture, 8, 8);
            Assert.AreEqual(8, resized.width);
            Assert.AreEqual(8, resized.height);
            Object.DestroyImmediate(resized);
        }

        [Test]
        public void Resize_縮小も動作する()
        {
            var resized = ImageHelper.Resize(_testTexture, 2, 2);
            Assert.AreEqual(2, resized.width);
            Assert.AreEqual(2, resized.height);
            Object.DestroyImmediate(resized);
        }

        [Test]
        public void ClassMapToTexture_出力サイズが正しい()
        {
            var classIds = new int[] { 0, 1, 2, 3 };
            var tex = ImageHelper.ClassMapToTexture(classIds, 2, 2, 4);
            Assert.AreEqual(2, tex.width);
            Assert.AreEqual(2, tex.height);
            Object.DestroyImmediate(tex);
        }

        [Test]
        public void ClassMapToTexture_異なるクラスは異なる色()
        {
            var classIds = new int[] { 0, 1, 2, 3 };
            var tex = ImageHelper.ClassMapToTexture(classIds, 2, 2, 4);
            var pixels = tex.GetPixels32();
            // 全ピクセルが異なる色であること（4クラスは十分離れた色相）
            for (int i = 0; i < pixels.Length; i++)
            {
                for (int j = i + 1; j < pixels.Length; j++)
                {
                    // 少なくとも1チャネルが異なること
                    bool different = pixels[i].r != pixels[j].r ||
                                     pixels[i].g != pixels[j].g ||
                                     pixels[i].b != pixels[j].b;
                    Assert.IsTrue(different, $"ピクセル {i} と {j} が同じ色");
                }
            }
            Object.DestroyImmediate(tex);
        }
    }
}
