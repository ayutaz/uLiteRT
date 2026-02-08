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

// LiteRT Unity バインディング — 新規列挙型検証テスト

using NUnit.Framework;

namespace LiteRT.Tests
{
    /// <summary>
    /// 新規追加された列挙型の値が C ヘッダーと一致するか検証するテスト。
    /// </summary>
    public class NewEnumTests
    {
        [Test]
        public void LiteRtGpuBackend_Values()
        {
            Assert.AreEqual(0, (int)LiteRtGpuBackend.Automatic);
            Assert.AreEqual(1, (int)LiteRtGpuBackend.OpenCl);
            Assert.AreEqual(2, (int)LiteRtGpuBackend.WebGpu);
            Assert.AreEqual(3, (int)LiteRtGpuBackend.OpenGl);
        }

        [Test]
        public void LiteRtGpuPriority_Values()
        {
            Assert.AreEqual(0, (int)LiteRtGpuPriority.Default);
            Assert.AreEqual(1, (int)LiteRtGpuPriority.Low);
            Assert.AreEqual(2, (int)LiteRtGpuPriority.Normal);
            Assert.AreEqual(3, (int)LiteRtGpuPriority.High);
        }

        [Test]
        public void LiteRtDelegatePrecision_Values()
        {
            Assert.AreEqual(0, (int)LiteRtDelegatePrecision.Default);
            Assert.AreEqual(1, (int)LiteRtDelegatePrecision.Fp16);
            Assert.AreEqual(2, (int)LiteRtDelegatePrecision.Fp32);
        }

        [Test]
        public void LiteRtGpuWaitType_Values()
        {
            Assert.AreEqual(0, (int)LiteRtGpuWaitType.Default);
            Assert.AreEqual(1, (int)LiteRtGpuWaitType.Passive);
            Assert.AreEqual(2, (int)LiteRtGpuWaitType.Active);
            Assert.AreEqual(3, (int)LiteRtGpuWaitType.DoNotWait);
        }

        [Test]
        public void LiteRtErrorReporterMode_Values()
        {
            Assert.AreEqual(0, (int)LiteRtErrorReporterMode.None);
            Assert.AreEqual(1, (int)LiteRtErrorReporterMode.Stderr);
            Assert.AreEqual(2, (int)LiteRtErrorReporterMode.Buffer);
        }

        [Test]
        public void LiteRtDelegateBufferStorageType_Values()
        {
            Assert.AreEqual(0, (int)LiteRtDelegateBufferStorageType.Default);
            Assert.AreEqual(1, (int)LiteRtDelegateBufferStorageType.Buffer);
            Assert.AreEqual(2, (int)LiteRtDelegateBufferStorageType.Texture2D);
        }
    }
}
