// LiteRT Unity バインディング — CPU 固有オプション

using System;

namespace LiteRT
{
    /// <summary>
    /// CPU 固有のコンパイルオプション。
    /// LiteRtOptions.AddOpaqueOptions() で追加することで所有権が移譲される。
    /// このクラスはスレッドセーフではない。
    /// </summary>
    public sealed class CpuOptions : IDisposable
    {
        internal IntPtr Handle { get; private set; }
        private bool _disposed;

        public CpuOptions()
        {
            LiteRtException.CheckStatus(
                Native.LiteRtCreateCpuOptions(out var handle));
            Handle = handle;
        }

        /// <summary>
        /// CPU スレッド数を設定する。
        /// 内部で LiteRtFindCpuOptions → LiteRtSetCpuOptionsNumThread を呼び出す。
        /// </summary>
        /// <param name="numThreads">使用するスレッド数。-1 でデフォルト。</param>
        public CpuOptions SetNumThreads(int numThreads)
        {
            ThrowIfDisposed();
            LiteRtException.CheckStatus(
                Native.LiteRtFindCpuOptions(Handle, out var cpuOptions));
            LiteRtException.CheckStatus(
                Native.LiteRtSetCpuOptionsNumThread(cpuOptions, numThreads));
            return this;
        }

        /// <summary>オプションが有効（Dispose 済みでない）かどうか。</summary>
        public bool IsValid => !_disposed && Handle != IntPtr.Zero;

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CpuOptions));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Handle = IntPtr.Zero;
        }
    }
}
