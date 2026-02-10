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

// LiteRT Unity バインディング — 構造体サイズ検証テスト

using System.Runtime.InteropServices;
using NUnit.Framework;

namespace LiteRT.Tests
{
    /// <summary>
    /// C# 構造体のマーシャリングサイズが C 側と一致するか検証するテスト。
    /// ネイティブライブラリなしで実行可能（EditMode テスト）。
    /// </summary>
    public class StructLayoutTests
    {
        [Test]
        public void LiteRtLayout_SizeMatchesNativeABI()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            // MSVC ABI: uint(4) + padding(4) + int32_t[8](32) + uint32_t[8](32) = 72
            const int expected = 72;
#else
            // GCC/Clang ABI: uint(4) + int32_t[8](32) + uint32_t[8](32) = 68
            const int expected = 68;
#endif
            int size = Marshal.SizeOf<LiteRtLayout>();
            Assert.AreEqual(expected, size,
                $"LiteRtLayout のサイズが想定と異なります: {size} bytes (期待値: {expected})");
        }

        [Test]
        public void LiteRtRankedTensorType_SizeMatchesNativeABI()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            // LiteRtElementType(4) + LiteRtLayout(72) = 76
            const int expected = 76;
#else
            // LiteRtElementType(4) + LiteRtLayout(68) = 72
            const int expected = 72;
#endif
            int size = Marshal.SizeOf<LiteRtRankedTensorType>();
            Assert.AreEqual(expected, size,
                $"LiteRtRankedTensorType のサイズが想定と異なります: {size} bytes (期待値: {expected})");
        }

        [Test]
        public void LiteRtUnrankedTensorType_SizeIs4Bytes()
        {
            int size = Marshal.SizeOf<LiteRtUnrankedTensorType>();
            Assert.AreEqual(4, size,
                $"LiteRtUnrankedTensorType のサイズが想定と異なります: {size} bytes (期待値: 4)");
        }

        [Test]
        public void LiteRtQuantizationPerTensor_SizeIs16Bytes()
        {
            // float(4) + パディング(4) + int64_t(8) = 16
            int size = Marshal.SizeOf<LiteRtQuantizationPerTensor>();
            Assert.AreEqual(16, size,
                $"LiteRtQuantizationPerTensor のサイズが想定と異なります: {size} bytes (期待値: 16)");
        }

        [Test]
        public void LiteRtApiVersion_SizeIs12Bytes()
        {
            // int(4) * 3 = 12
            int size = Marshal.SizeOf<LiteRtApiVersion>();
            Assert.AreEqual(12, size,
                $"LiteRtApiVersion のサイズが想定と異なります: {size} bytes (期待値: 12)");
        }

        [Test]
        public void LiteRtLayout_RankAndHasStrides_BitField()
        {
            // デフォルト構築で rank=0, has_strides=false であることを確認
            var layout = new LiteRtLayout();
            Assert.AreEqual(0, layout.Rank);
            Assert.IsFalse(layout.HasStrides);
        }
    }
}
