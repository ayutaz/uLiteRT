// LiteRT Unity バインディング — 環境ラッパー

using System;

namespace LiteRT
{
    /// <summary>
    /// LiteRT ランタイム環境。全ての LiteRT 操作の起点となるオブジェクト。
    /// 使用後は必ず Dispose すること。
    /// </summary>
    public sealed class LiteRtEnvironment : IDisposable
    {
        internal IntPtr Handle { get; private set; }
        private bool _disposed;

        /// <summary>
        /// デフォルト設定で環境を作成する。
        /// </summary>
        public LiteRtEnvironment()
        {
            LiteRtException.CheckStatus(
                Native.LiteRtCreateEnvironment(0, IntPtr.Zero, out var handle));
            Handle = handle;
        }

        /// <summary>
        /// 環境が有効（Dispose 済みでない）かどうか。
        /// </summary>
        public bool IsValid => !_disposed && Handle != IntPtr.Zero;

        internal void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LiteRtEnvironment));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (Handle != IntPtr.Zero)
            {
                Native.LiteRtDestroyEnvironment(Handle);
                Handle = IntPtr.Zero;
            }
        }
    }
}
