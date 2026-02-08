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

// LiteRT Unity バインディング — 列挙型検証テスト

using NUnit.Framework;

namespace LiteRT.Tests
{
    /// <summary>
    /// 列挙型の値が C ヘッダーと一致するか検証するテスト。
    /// </summary>
    public class EnumTests
    {
        [Test]
        public void LiteRtStatus_Values()
        {
            Assert.AreEqual(0, (int)LiteRtStatus.Ok);
            Assert.AreEqual(1, (int)LiteRtStatus.ErrorInvalidArgument);
            Assert.AreEqual(100, (int)LiteRtStatus.Cancelled);
            Assert.AreEqual(500, (int)LiteRtStatus.ErrorFileIO);
            Assert.AreEqual(1000, (int)LiteRtStatus.ErrorIndexOOB);
            Assert.AreEqual(1500, (int)LiteRtStatus.ErrorInvalidToolConfig);
        }

        [Test]
        public void LiteRtElementType_Values()
        {
            Assert.AreEqual(0, (int)LiteRtElementType.None);
            Assert.AreEqual(1, (int)LiteRtElementType.Float32);
            Assert.AreEqual(9, (int)LiteRtElementType.Int8);
            Assert.AreEqual(10, (int)LiteRtElementType.Float16);
            Assert.AreEqual(20, (int)LiteRtElementType.Int2);
        }

        [Test]
        public void LiteRtHwAccelerators_Flags()
        {
            Assert.AreEqual(0, (int)LiteRtHwAccelerators.None);
            Assert.AreEqual(1, (int)LiteRtHwAccelerators.Cpu);
            Assert.AreEqual(2, (int)LiteRtHwAccelerators.Gpu);
            Assert.AreEqual(4, (int)LiteRtHwAccelerators.Npu);

            // ビットフラグの合成確認
            var cpuGpu = LiteRtHwAccelerators.Cpu | LiteRtHwAccelerators.Gpu;
            Assert.AreEqual(3, (int)cpuGpu);
        }

        [Test]
        public void LiteRtTensorBufferType_Values()
        {
            Assert.AreEqual(0, (int)LiteRtTensorBufferType.Unknown);
            Assert.AreEqual(1, (int)LiteRtTensorBufferType.HostMemory);
            Assert.AreEqual(6, (int)LiteRtTensorBufferType.GlBuffer);
            Assert.AreEqual(10, (int)LiteRtTensorBufferType.OpenClBuffer);
            Assert.AreEqual(30, (int)LiteRtTensorBufferType.MetalBuffer);
            Assert.AreEqual(40, (int)LiteRtTensorBufferType.VulkanBuffer);
        }

        [Test]
        public void LiteRtTensorBufferLockMode_Values()
        {
            Assert.AreEqual(0, (int)LiteRtTensorBufferLockMode.Read);
            Assert.AreEqual(1, (int)LiteRtTensorBufferLockMode.Write);
            Assert.AreEqual(2, (int)LiteRtTensorBufferLockMode.ReadWrite);
        }

        [Test]
        public void LiteRtTensorTypeId_Values()
        {
            Assert.AreEqual(0, (int)LiteRtTensorTypeId.Ranked);
            Assert.AreEqual(1, (int)LiteRtTensorTypeId.Unranked);
        }

        [Test]
        public void LiteRtQuantizationTypeId_Values()
        {
            Assert.AreEqual(0, (int)LiteRtQuantizationTypeId.None);
            Assert.AreEqual(1, (int)LiteRtQuantizationTypeId.PerTensor);
            Assert.AreEqual(2, (int)LiteRtQuantizationTypeId.PerChannel);
            Assert.AreEqual(3, (int)LiteRtQuantizationTypeId.BlockWise);
        }
    }
}
