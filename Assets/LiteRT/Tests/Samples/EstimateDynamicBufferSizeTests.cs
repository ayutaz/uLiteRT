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

// LiteRT Unity バインディング — 動的バッファサイズ推定テスト

using NUnit.Framework;
using LiteRT;

namespace LiteRT.Tests.Samples
{
    /// <summary>
    /// LiteRtTensorBuffer.EstimateDynamicBufferSize の単体テスト。
    /// 動的出力テンソルのバッファサイズ推定ロジックを検証する。ネイティブライブラリ不要。
    /// </summary>
    public class EstimateDynamicBufferSizeTests
    {
        [Test]
        public void Float32_Dim1LessThanMaxFrames_UsesMaxFrames()
        {
            // [1, 10, 80], maxFrames=500 → dim[1]=10 < 500 → 1*500*80*4 = 160000
            long result = LiteRtTensorBuffer.EstimateDynamicBufferSize(
                LiteRtElementType.Float32, new[] { 1, 10, 80 }, 500);
            Assert.AreEqual(160000L, result);
        }

        [Test]
        public void Float32_Dim1GreaterThanMaxFrames_KeepsDim1()
        {
            // [1, 600, 80], maxFrames=500 → dim[1]=600 >= 500 → 1*600*80*4 = 192000
            long result = LiteRtTensorBuffer.EstimateDynamicBufferSize(
                LiteRtElementType.Float32, new[] { 1, 600, 80 }, 500);
            Assert.AreEqual(192000L, result);
        }

        [Test]
        public void Int32_ZeroDim1_ReplacedByMaxFrames()
        {
            // [1, 0, 80], maxFrames=100 → dim[1]=0 < 100 → 1*100*80*4 = 32000
            long result = LiteRtTensorBuffer.EstimateDynamicBufferSize(
                LiteRtElementType.Int32, new[] { 1, 0, 80 }, 100);
            Assert.AreEqual(32000L, result);
        }

        [Test]
        public void EmptyDims_ReturnsZero()
        {
            long result = LiteRtTensorBuffer.EstimateDynamicBufferSize(
                LiteRtElementType.Float32, new int[0], 500);
            Assert.AreEqual(0L, result);
        }

        [Test]
        public void NegativeDim1_ReplacedByMaxFrames()
        {
            // [1, -1, 80], maxFrames=50 → dim[1]=-1 < 50 → dim[1]=50
            // dim[0]=1, dim[2]=80 → 1*50*80*4 = 16000
            long result = LiteRtTensorBuffer.EstimateDynamicBufferSize(
                LiteRtElementType.Float32, new[] { 1, -1, 80 }, 50);
            Assert.AreEqual(16000L, result);
        }

        [Test]
        public void Dim0Zero_CorrectedToOne()
        {
            // [0, 10, 80], maxFrames=5 → dim[0]=0 → 1 に補正
            // dim[1]=10 >= 5 → そのまま → 1*10*80*4 = 3200
            long result = LiteRtTensorBuffer.EstimateDynamicBufferSize(
                LiteRtElementType.Float32, new[] { 0, 10, 80 }, 5);
            Assert.AreEqual(3200L, result);
        }

        [Test]
        public void Float64_ElementSize8()
        {
            // [1, 10, 80], maxFrames=500 → 1*500*80*8 = 320000
            long result = LiteRtTensorBuffer.EstimateDynamicBufferSize(
                LiteRtElementType.Float64, new[] { 1, 10, 80 }, 500);
            Assert.AreEqual(320000L, result);
        }

        [Test]
        public void Int64_ElementSize8()
        {
            // [1, 20, 40], maxFrames=10 → dim[1]=20 >= 10 → 1*20*40*8 = 6400
            long result = LiteRtTensorBuffer.EstimateDynamicBufferSize(
                LiteRtElementType.Int64, new[] { 1, 20, 40 }, 10);
            Assert.AreEqual(6400L, result);
        }

        [Test]
        public void UnknownType_DefaultElementSize4()
        {
            // Bool 等は default → elementSize=4
            // [1, 5], maxFrames=3 → dim[1]=5 >= 3 → 1*5*4 = 20
            long result = LiteRtTensorBuffer.EstimateDynamicBufferSize(
                LiteRtElementType.Bool, new[] { 1, 5 }, 3);
            Assert.AreEqual(20L, result);
        }

        [Test]
        public void SingleDimension_NoTimeAxis()
        {
            // [10], maxFrames=100 → dim[0]=10, 0以下でないのでそのまま → 10*4 = 40
            long result = LiteRtTensorBuffer.EstimateDynamicBufferSize(
                LiteRtElementType.Float32, new[] { 10 }, 100);
            Assert.AreEqual(40L, result);
        }

        [Test]
        public void Dim1EqualToMaxFrames_KeepsDim1()
        {
            // [1, 500, 80], maxFrames=500 → dim[1]=500 == maxFrames → dim[1] < maxFrames は false → そのまま
            long result = LiteRtTensorBuffer.EstimateDynamicBufferSize(
                LiteRtElementType.Float32, new[] { 1, 500, 80 }, 500);
            Assert.AreEqual(160000L, result);
        }
    }
}
