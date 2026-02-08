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

// Texture2D ⇔ float[]/byte[] 変換ヘルパー

using UnityEngine;

namespace LiteRT.Samples
{
    /// <summary>
    /// Texture2D とテンソルデータの相互変換を行うヘルパー。
    /// </summary>
    public static class ImageHelper
    {
        /// <summary>
        /// Texture2D を指定サイズにリサイズする（RenderTexture 経由）。
        /// </summary>
        /// <param name="source">元画像。</param>
        /// <param name="width">出力幅。</param>
        /// <param name="height">出力高さ。</param>
        /// <returns>リサイズされた Texture2D（呼び出し元が破棄責任を持つ）。</returns>
        public static Texture2D Resize(Texture2D source, int width, int height)
        {
            var rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            var prevActive = RenderTexture.active;
            RenderTexture.active = rt;

            Graphics.Blit(source, rt);

            var resized = new Texture2D(width, height, TextureFormat.RGBA32, false);
            resized.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            resized.Apply();

            RenderTexture.active = prevActive;
            RenderTexture.ReleaseTemporary(rt);

            return resized;
        }

        /// <summary>
        /// Texture2D を float[] に変換する（正規化付き、RGB チャネル順）。
        /// 出力形式: [1, height, width, 3] のフラットな float[]。
        /// </summary>
        /// <param name="texture">入力画像。</param>
        /// <param name="mean">正規化の平均値。</param>
        /// <param name="std">正規化の標準偏差。</param>
        /// <returns>正規化された float 配列。</returns>
        public static float[] TextureToFloatArray(Texture2D texture, float mean = 0f, float std = 1f)
        {
            int width = texture.width;
            int height = texture.height;
            var pixels = texture.GetPixels32();

            // NHWC 形式: [1, H, W, 3]
            var result = new float[height * width * 3];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Unity の GetPixels32 は左下起点、TFLite は左上起点
                    var pixel = pixels[(height - 1 - y) * width + x];
                    int idx = (y * width + x) * 3;
                    result[idx + 0] = (pixel.r / 255f - mean) / std;
                    result[idx + 1] = (pixel.g / 255f - mean) / std;
                    result[idx + 2] = (pixel.b / 255f - mean) / std;
                }
            }

            return result;
        }

        /// <summary>
        /// Texture2D を byte[] に変換する（uint8 モデル用、RGB チャネル順）。
        /// 出力形式: [1, height, width, 3] のフラットな byte[]。
        /// </summary>
        /// <param name="texture">入力画像。</param>
        /// <returns>byte 配列（0-255）。</returns>
        public static byte[] TextureToByteArray(Texture2D texture)
        {
            int width = texture.width;
            int height = texture.height;
            var pixels = texture.GetPixels32();

            // NHWC 形式: [1, H, W, 3]
            var result = new byte[height * width * 3];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var pixel = pixels[(height - 1 - y) * width + x];
                    int idx = (y * width + x) * 3;
                    result[idx + 0] = pixel.r;
                    result[idx + 1] = pixel.g;
                    result[idx + 2] = pixel.b;
                }
            }

            return result;
        }

        /// <summary>
        /// float[] を Texture2D に変換する（RGB、[0, 1] 範囲をクランプ）。
        /// 入力形式: [height, width, 3] のフラットな float[] を想定。
        /// </summary>
        /// <param name="data">float 配列。</param>
        /// <param name="width">画像幅。</param>
        /// <param name="height">画像高さ。</param>
        /// <returns>Texture2D（呼び出し元が破棄責任を持つ）。</returns>
        public static Texture2D FloatArrayToTexture(float[] data, int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int srcIdx = (y * width + x) * 3;
                    byte r = (byte)(Mathf.Clamp01(data[srcIdx + 0]) * 255f);
                    byte g = (byte)(Mathf.Clamp01(data[srcIdx + 1]) * 255f);
                    byte b = (byte)(Mathf.Clamp01(data[srcIdx + 2]) * 255f);

                    // 出力は左下起点
                    pixels[(height - 1 - y) * width + x] = new Color32(r, g, b, 255);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// セグメンテーションマスク（クラス ID 配列）をカラーマップ Texture2D に変換する。
        /// </summary>
        /// <param name="classIds">各ピクセルのクラス ID 配列 [height * width]。</param>
        /// <param name="width">画像幅。</param>
        /// <param name="height">画像高さ。</param>
        /// <param name="numClasses">クラス数。</param>
        /// <returns>カラーマップ Texture2D。</returns>
        public static Texture2D ClassMapToTexture(int[] classIds, int width, int height, int numClasses)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];
            var palette = GeneratePalette(numClasses);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int srcIdx = y * width + x;
                    int classId = classIds[srcIdx];
                    var color = classId >= 0 && classId < palette.Length ? palette[classId] : new Color32(0, 0, 0, 255);
                    // 左下起点
                    pixels[(height - 1 - y) * width + x] = color;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// 指定数のカラーパレットを生成する（HSV ベースで均等分散）。
        /// </summary>
        private static Color32[] GeneratePalette(int count)
        {
            var palette = new Color32[count];
            for (int i = 0; i < count; i++)
            {
                float hue = (float)i / count;
                var color = Color.HSVToRGB(hue, 0.8f, 0.9f);
                palette[i] = new Color32(
                    (byte)(color.r * 255), (byte)(color.g * 255), (byte)(color.b * 255), 180);
            }
            return palette;
        }
    }
}
