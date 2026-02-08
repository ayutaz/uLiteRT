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
