// LiteRT Unity バインディング — GPU 固有オプション

using System;

namespace LiteRT
{
    /// <summary>
    /// GPU 固有のコンパイルオプション。ビルダーパターンで設定する。
    /// LiteRtOptions.AddOpaqueOptions() で追加することで所有権が移譲される。
    /// このクラスはスレッドセーフではない。
    /// </summary>
    public sealed class GpuOptions : IDisposable
    {
        internal IntPtr Handle { get; private set; }
        private bool _disposed;
        private bool _ownershipTransferred;

        public GpuOptions()
        {
            LiteRtException.CheckStatus(
                Native.LiteRtCreateGpuOptions(out var handle));
            Handle = handle;
        }

        /// <summary>GPU バックエンドを設定する。</summary>
        public GpuOptions SetGpuBackend(LiteRtGpuBackend backend)
        {
            ThrowIfDisposed();
            LiteRtException.CheckStatus(
                Native.LiteRtSetGpuOptionsGpuBackend(Handle, backend));
            return this;
        }

        /// <summary>計算精度を設定する。</summary>
        public GpuOptions SetPrecision(LiteRtDelegatePrecision precision)
        {
            ThrowIfDisposed();
            LiteRtException.CheckStatus(
                Native.LiteRtSetGpuAcceleratorCompilationOptionsPrecision(Handle, precision));
            return this;
        }

        /// <summary>GPU 実行優先度を設定する。</summary>
        public GpuOptions SetGpuPriority(LiteRtGpuPriority priority)
        {
            ThrowIfDisposed();
            LiteRtException.CheckStatus(
                Native.LiteRtSetGpuOptionsGpuPriority(Handle, priority));
            return this;
        }

        /// <summary>外部テンソルモードを設定する。</summary>
        public GpuOptions SetExternalTensorsMode(bool enable)
        {
            ThrowIfDisposed();
            LiteRtException.CheckStatus(
                Native.LiteRtSetGpuOptionsExternalTensorsMode(Handle, enable));
            return this;
        }

        /// <summary>コンパイル済みモデルのシリアライゼーションディレクトリを設定する。</summary>
        public GpuOptions SetSerializationDir(string dir)
        {
            ThrowIfDisposed();
            if (dir == null) throw new ArgumentNullException(nameof(dir));
            LiteRtException.CheckStatus(
                Native.LiteRtSetGpuAcceleratorCompilationOptionsSerializationDir(Handle, dir));
            return this;
        }

        /// <summary>モデルキャッシュキーを設定する。</summary>
        public GpuOptions SetModelCacheKey(string key)
        {
            ThrowIfDisposed();
            if (key == null) throw new ArgumentNullException(nameof(key));
            LiteRtException.CheckStatus(
                Native.LiteRtSetGpuAcceleratorCompilationOptionsModelCacheKey(Handle, key));
            return this;
        }

        /// <summary>単一デリゲートへの完全委任ヒントを設定する。</summary>
        public GpuOptions SetHintFullyDelegated(bool hint)
        {
            ThrowIfDisposed();
            LiteRtException.CheckStatus(
                Native.LiteRtSetGpuOptionsHintFullyDelegatedToSingleDelegate(Handle, hint));
            return this;
        }

        /// <summary>
        /// LiteRtOptions に追加して所有権を移譲する。
        /// 移譲後は Dispose しても native destroy は呼ばれない。
        /// </summary>
        internal void MarkOwnershipTransferred()
        {
            _ownershipTransferred = true;
        }

        /// <summary>オプションが有効（Dispose 済みでない）かどうか。</summary>
        public bool IsValid => !_disposed && Handle != IntPtr.Zero;

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GpuOptions));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // AddOpaqueOptions で所有権が移譲された場合は native destroy 不要
            // GPU options には個別の Destroy 関数がないため、
            // Options 側で一括破棄される
            Handle = IntPtr.Zero;
        }
    }
}
