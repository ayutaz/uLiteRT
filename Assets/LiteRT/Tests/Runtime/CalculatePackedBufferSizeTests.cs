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

// LiteRT Unity バインディング — バッファサイズ計算テスト

using System;
using NUnit.Framework;

namespace LiteRT.Tests
{
    /// <summary>
    /// LiteRtTensorBuffer.CalculatePackedBufferSize の単体テスト。
    /// 全要素型のバイトサイズ計算を検証する。ネイティブライブラリ不要。
    /// </summary>
    public class CalculatePackedBufferSizeTests
    {
        [Test]
        public void Float32_3D_ReturnsCorrectSize()
        {
            // [1, 10, 80] × 4 bytes = 3200
            var layout = LiteRtLayout.Create(1, 10, 80);
            long result = LiteRtTensorBuffer.CalculatePackedBufferSize(
                LiteRtElementType.Float32, layout);
            Assert.AreEqual(3200L, result);
        }

        [Test]
        public void Int32_2D_ReturnsCorrectSize()
        {
            // [1, 5] × 4 bytes = 20
            var layout = LiteRtLayout.Create(1, 5);
            long result = LiteRtTensorBuffer.CalculatePackedBufferSize(
                LiteRtElementType.Int32, layout);
            Assert.AreEqual(20L, result);
        }

        [Test]
        public void Float64_2D_ReturnsCorrectSize()
        {
            // [2, 3] × 8 bytes = 48
            var layout = LiteRtLayout.Create(2, 3);
            long result = LiteRtTensorBuffer.CalculatePackedBufferSize(
                LiteRtElementType.Float64, layout);
            Assert.AreEqual(48L, result);
        }

        [Test]
        public void Int4_SubByte_ReturnsPackedSize()
        {
            // Int4: num=1, denom=2 → ceil(8 * 1 / 2) = 4
            var layout = LiteRtLayout.Create(1, 8);
            long result = LiteRtTensorBuffer.CalculatePackedBufferSize(
                LiteRtElementType.Int4, layout);
            Assert.AreEqual(4L, result);
        }

        [Test]
        public void Int2_SubByte_ReturnsPackedSize()
        {
            // Int2: num=1, denom=4 → ceil(8 * 1 / 4) = 2
            var layout = LiteRtLayout.Create(1, 8);
            long result = LiteRtTensorBuffer.CalculatePackedBufferSize(
                LiteRtElementType.Int2, layout);
            Assert.AreEqual(2L, result);
        }

        [Test]
        public void Int4_OddElements_RoundsUp()
        {
            // Int4: [1, 7] → totalElements=7, (7*1+2-1)/2 = 4
            var layout = LiteRtLayout.Create(1, 7);
            long result = LiteRtTensorBuffer.CalculatePackedBufferSize(
                LiteRtElementType.Int4, layout);
            Assert.AreEqual(4L, result);
        }

        [Test]
        public void Int2_OddElements_RoundsUp()
        {
            // Int2: [1, 5] → totalElements=5, (5*1+4-1)/4 = 2
            var layout = LiteRtLayout.Create(1, 5);
            long result = LiteRtTensorBuffer.CalculatePackedBufferSize(
                LiteRtElementType.Int2, layout);
            Assert.AreEqual(2L, result);
        }

        [Test]
        public void EmptyDimensions_ReturnsZero()
        {
            var layout = LiteRtLayout.Create();
            long result = LiteRtTensorBuffer.CalculatePackedBufferSize(
                LiteRtElementType.Float32, layout);
            Assert.AreEqual(0L, result);
        }

        [Test]
        public void SingleDimension_Float32_ReturnsCorrectSize()
        {
            // [5] × 4 bytes = 20
            var layout = LiteRtLayout.Create(5);
            long result = LiteRtTensorBuffer.CalculatePackedBufferSize(
                LiteRtElementType.Float32, layout);
            Assert.AreEqual(20L, result);
        }

        [Test]
        public void Bool_1Byte_ReturnsCorrectSize()
        {
            // Bool: num=1, denom=1 → [1, 10] = 10
            var layout = LiteRtLayout.Create(1, 10);
            long result = LiteRtTensorBuffer.CalculatePackedBufferSize(
                LiteRtElementType.Bool, layout);
            Assert.AreEqual(10L, result);
        }

        [Test]
        public void Int8_1Byte_ReturnsCorrectSize()
        {
            // Int8: num=1, denom=1 → [2, 4] = 8
            var layout = LiteRtLayout.Create(2, 4);
            long result = LiteRtTensorBuffer.CalculatePackedBufferSize(
                LiteRtElementType.Int8, layout);
            Assert.AreEqual(8L, result);
        }

        [Test]
        public void UInt8_1Byte_ReturnsCorrectSize()
        {
            // UInt8: num=1, denom=1 → [3, 3] = 9
            var layout = LiteRtLayout.Create(3, 3);
            long result = LiteRtTensorBuffer.CalculatePackedBufferSize(
                LiteRtElementType.UInt8, layout);
            Assert.AreEqual(9L, result);
        }

        [Test]
        public void Int16_2Bytes_ReturnsCorrectSize()
        {
            // Int16: num=2, denom=1 → [1, 4] × 2 = 8
            var layout = LiteRtLayout.Create(1, 4);
            long result = LiteRtTensorBuffer.CalculatePackedBufferSize(
                LiteRtElementType.Int16, layout);
            Assert.AreEqual(8L, result);
        }

        [Test]
        public void Float16_2Bytes_ReturnsCorrectSize()
        {
            // Float16: num=2, denom=1 → [1, 10] × 2 = 20
            var layout = LiteRtLayout.Create(1, 10);
            long result = LiteRtTensorBuffer.CalculatePackedBufferSize(
                LiteRtElementType.Float16, layout);
            Assert.AreEqual(20L, result);
        }

        [Test]
        public void BFloat16_2Bytes_ReturnsCorrectSize()
        {
            // BFloat16: num=2, denom=1 → [2, 5] × 2 = 20
            var layout = LiteRtLayout.Create(2, 5);
            long result = LiteRtTensorBuffer.CalculatePackedBufferSize(
                LiteRtElementType.BFloat16, layout);
            Assert.AreEqual(20L, result);
        }

        [Test]
        public void Int64_8Bytes_ReturnsCorrectSize()
        {
            // Int64: num=8, denom=1 → [1, 3] × 8 = 24
            var layout = LiteRtLayout.Create(1, 3);
            long result = LiteRtTensorBuffer.CalculatePackedBufferSize(
                LiteRtElementType.Int64, layout);
            Assert.AreEqual(24L, result);
        }

        [Test]
        public void Complex64_4Bytes_ReturnsCorrectSize()
        {
            // Complex64: num=4, denom=1 → [1, 2] × 4 = 8
            var layout = LiteRtLayout.Create(1, 2);
            long result = LiteRtTensorBuffer.CalculatePackedBufferSize(
                LiteRtElementType.Complex64, layout);
            Assert.AreEqual(8L, result);
        }

        [Test]
        public void Complex128_8Bytes_ReturnsCorrectSize()
        {
            // Complex128: num=8, denom=1 → [1, 2] × 8 = 16
            var layout = LiteRtLayout.Create(1, 2);
            long result = LiteRtTensorBuffer.CalculatePackedBufferSize(
                LiteRtElementType.Complex128, layout);
            Assert.AreEqual(16L, result);
        }

        [Test]
        public void UnsupportedType_ThrowsArgumentException()
        {
            // None 型はサポートされていない
            var layout = LiteRtLayout.Create(1, 2);
            Assert.Throws<ArgumentException>(() =>
                LiteRtTensorBuffer.CalculatePackedBufferSize(
                    LiteRtElementType.None, layout));
        }
    }
}
