// LiteRT Unity バインディング — ランタイムオプション

using System;

namespace LiteRT
{
    /// <summary>
    /// ランタイムオプション（プロファイリング、エラーレポーター等）。
    /// LiteRtOptions.AddOpaqueOptions() で追加することで所有権が移譲される。
    /// このクラスはスレッドセーフではない。
    /// </summary>
    public sealed class RuntimeOptions : IDisposable
    {
        internal IntPtr Handle { get; private set; }
        private bool _disposed;

        public RuntimeOptions()
        {
            LiteRtException.CheckStatus(
                Native.LiteRtCreateRuntimeOptions(out var handle));
            Handle = handle;
        }

        /// <summary>プロファイリングの有効/無効を設定する。</summary>
        public RuntimeOptions SetEnableProfiling(bool enable)
        {
            ThrowIfDisposed();
            LiteRtException.CheckStatus(
                Native.LiteRtFindRuntimeOptions(Handle, out var runtimeOptions));
            LiteRtException.CheckStatus(
                Native.LiteRtSetRuntimeOptionsEnableProfiling(runtimeOptions, enable));
            return this;
        }

        /// <summary>エラーレポータのモードを設定する。</summary>
        public RuntimeOptions SetErrorReporterMode(LiteRtErrorReporterMode mode)
        {
            ThrowIfDisposed();
            LiteRtException.CheckStatus(
                Native.LiteRtFindRuntimeOptions(Handle, out var runtimeOptions));
            LiteRtException.CheckStatus(
                Native.LiteRtSetRuntimeOptionsErrorReporterMode(runtimeOptions, mode));
            return this;
        }

        /// <summary>オプションが有効（Dispose 済みでない）かどうか。</summary>
        public bool IsValid => !_disposed && Handle != IntPtr.Zero;

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RuntimeOptions));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Handle = IntPtr.Zero;
        }
    }
}
