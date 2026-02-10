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

// LiteRT Unity バインディング — 構造体操作テスト

using System;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace LiteRT.Tests
{
    /// <summary>
    /// LiteRtLayout のビットフィールド操作と配列アクセスのテスト。
    /// ネイティブライブラリなしで実行可能。
    /// </summary>
    public class StructOperationTests
    {
        [Test]
        public void LiteRtLayout_Rank_ExtractsLower7Bits()
        {
            // rank=3 を設定 (下位7ビット = 0x03)
            var layout = CreateLayoutWithRankAndFlags(3, false);
            Assert.AreEqual(3, layout.Rank);
        }

        [Test]
        public void LiteRtLayout_Rank_MaxValue127()
        {
            // rank の最大値 = 127 (7ビット)
            var layout = CreateLayoutWithRankAndFlags(127, false);
            Assert.AreEqual(127, layout.Rank);
        }

        [Test]
        public void LiteRtLayout_HasStrides_ExtractsBit7_True()
        {
            var layout = CreateLayoutWithRankAndFlags(0, true);
            Assert.IsTrue(layout.HasStrides);
        }

        [Test]
        public void LiteRtLayout_HasStrides_ExtractsBit7_False()
        {
            var layout = CreateLayoutWithRankAndFlags(5, false);
            Assert.IsFalse(layout.HasStrides);
        }

        [Test]
        public void LiteRtLayout_RankAndHasStrides_Combined()
        {
            // rank=4, has_strides=true
            var layout = CreateLayoutWithRankAndFlags(4, true);
            Assert.AreEqual(4, layout.Rank);
            Assert.IsTrue(layout.HasStrides);
        }

        [Test]
        public void LiteRtLayout_GetDimensions_ReturnsRankElements()
        {
            var layout = CreateLayoutWithRankAndFlags(3, false);
            layout.dimensions = new int[] { 1, 224, 224, 0, 0, 0, 0, 0 };
            var dims = layout.GetDimensions();
            Assert.AreEqual(3, dims.Length);
            Assert.AreEqual(1, dims[0]);
            Assert.AreEqual(224, dims[1]);
            Assert.AreEqual(224, dims[2]);
        }

        [Test]
        public void LiteRtLayout_GetDimensions_ZeroRank_ReturnsEmpty()
        {
            var layout = CreateLayoutWithRankAndFlags(0, false);
            layout.dimensions = new int[8];
            var dims = layout.GetDimensions();
            Assert.AreEqual(0, dims.Length);
        }

        [Test]
        public void LiteRtLayout_GetStrides_WithHasStrides_ReturnsValues()
        {
            var layout = CreateLayoutWithRankAndFlags(2, true);
            layout.dimensions = new int[8];
            layout.strides = new uint[] { 4, 1, 0, 0, 0, 0, 0, 0 };
            var strides = layout.GetStrides();
            Assert.AreEqual(2, strides.Length);
            Assert.AreEqual(4u, strides[0]);
            Assert.AreEqual(1u, strides[1]);
        }

        [Test]
        public void LiteRtLayout_GetStrides_WithoutHasStrides_ReturnsEmpty()
        {
            var layout = CreateLayoutWithRankAndFlags(2, false);
            layout.dimensions = new int[8];
            layout.strides = new uint[8];
            var strides = layout.GetStrides();
            Assert.AreEqual(0, strides.Length);
        }

        [Test]
        public void LiteRtQuantizationPerChannel_FieldOffsets()
        {
            // scales: offset 0 (IntPtr)
            // zeroPoints: offset IntPtr.Size
            // quantizedDimension: offset IntPtr.Size * 2
            // numChannels: offset IntPtr.Size * 2 + sizeof(int) (+ padding on 64bit)
            int scalesOffset = (int)Marshal.OffsetOf<LiteRtQuantizationPerChannel>(nameof(LiteRtQuantizationPerChannel.scales));
            int zeroPointsOffset = (int)Marshal.OffsetOf<LiteRtQuantizationPerChannel>(nameof(LiteRtQuantizationPerChannel.zeroPoints));
            Assert.AreEqual(0, scalesOffset);
            Assert.AreEqual(IntPtr.Size, zeroPointsOffset);
            Assert.IsTrue(zeroPointsOffset > scalesOffset, "zeroPoints は scales の後に配置されるべき");
        }

        // --- LiteRtLayout.Create テスト ---

        [Test]
        public void LiteRtLayout_Create_3D_ReturnsCorrectDimensions()
        {
            var layout = LiteRtLayout.Create(1, 10, 80);
            var dims = layout.GetDimensions();
            Assert.AreEqual(3, dims.Length);
            Assert.AreEqual(1, dims[0]);
            Assert.AreEqual(10, dims[1]);
            Assert.AreEqual(80, dims[2]);
            Assert.AreEqual(3, layout.Rank);
        }

        [Test]
        public void LiteRtLayout_Create_Empty_ReturnsZeroRank()
        {
            var layout = LiteRtLayout.Create();
            var dims = layout.GetDimensions();
            Assert.AreEqual(0, dims.Length);
            Assert.AreEqual(0, layout.Rank);
        }

        [Test]
        public void LiteRtLayout_Create_Single_ReturnsOneDimension()
        {
            var layout = LiteRtLayout.Create(1);
            var dims = layout.GetDimensions();
            Assert.AreEqual(1, dims.Length);
            Assert.AreEqual(1, dims[0]);
            Assert.AreEqual(1, layout.Rank);
        }

        [Test]
        public void LiteRtLayout_Create_MaxDimensions8_Succeeds()
        {
            var layout = LiteRtLayout.Create(1, 2, 3, 4, 5, 6, 7, 8);
            var dims = layout.GetDimensions();
            Assert.AreEqual(8, dims.Length);
            Assert.AreEqual(8, layout.Rank);
            for (int i = 0; i < 8; i++)
                Assert.AreEqual(i + 1, dims[i]);
        }

        [Test]
        public void LiteRtLayout_Create_Over8Dims_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                LiteRtLayout.Create(1, 2, 3, 4, 5, 6, 7, 8, 9));
        }

        [Test]
        public void LiteRtLayout_Create_NullDims_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                LiteRtLayout.Create(null));
        }

        [Test]
        public void LiteRtLayout_Create_DimensionsArrayPaddedWithZero()
        {
            // 3次元で作成し、残りの要素が 0 であることを確認
            var layout = LiteRtLayout.Create(10, 20, 30);
            Assert.AreEqual(0, layout.dimensions[3]);
            Assert.AreEqual(0, layout.dimensions[4]);
            Assert.AreEqual(0, layout.dimensions[7]);
        }

        /// <summary>
        /// テスト用ヘルパー: _rankAndFlags フィールドをリフレクションで設定して LiteRtLayout を作成する。
        /// </summary>
        private static LiteRtLayout CreateLayoutWithRankAndFlags(int rank, bool hasStrides)
        {
            uint rankBits = (uint)(rank & 0x7F);
#if !(UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
            // GCC/Clang ABI: rank と has_strides は同じ 4 バイトにパックされる
            if (hasStrides) rankBits |= 0x80;
#endif

            var layout = new LiteRtLayout();
            layout.dimensions = new int[8];
            layout.strides = new uint[8];

            // _rankAndFlags は private なので Marshal で設定
            int size = Marshal.SizeOf<LiteRtLayout>();
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(layout, ptr, false);
                Marshal.WriteInt32(ptr, 0, (int)rankBits); // 先頭4バイトに _rankAndFlags を書き込み
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                // MSVC ABI: has_strides は別のアロケーション単位（オフセット 4）のビット 0
                Marshal.WriteInt32(ptr, 4, hasStrides ? 1 : 0);
#endif
                layout = Marshal.PtrToStructure<LiteRtLayout>(ptr);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            return layout;
        }
    }
}
