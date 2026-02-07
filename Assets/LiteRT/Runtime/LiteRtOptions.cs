// LiteRT Unity バインディング — コンパイルオプションビルダー

using System;

namespace LiteRT
{
    /// <summary>
    /// モデルコンパイル時のオプション。ビルダーパターンで設定する。
    /// 使用後は必ず Dispose すること。
    /// </summary>
    public sealed class LiteRtOptions : IDisposable
    {
        internal IntPtr Handle { get; private set; }
        private bool _disposed;

        public LiteRtOptions()
        {
            LiteRtException.CheckStatus(
                Native.LiteRtCreateOptions(out var handle));
            Handle = handle;
        }

        /// <summary>
        /// 使用するハードウェアアクセラレータを設定する。
        /// </summary>
        /// <returns>メソッドチェーン用に自身を返す。</returns>
        public LiteRtOptions SetHardwareAccelerators(LiteRtHwAccelerators accelerators)
        {
            ThrowIfDisposed();
            LiteRtException.CheckStatus(
                Native.LiteRtSetOptionsHardwareAccelerators(Handle, (int)accelerators));
            return this;
        }

        /// <summary>
        /// 不透明オプション（GPU オプション等）を追加する。
        /// 追加したオプションの所有権は Options に移譲される。
        /// </summary>
        /// <returns>メソッドチェーン用に自身を返す。</returns>
        public LiteRtOptions AddOpaqueOptions(IntPtr opaqueOptions)
        {
            ThrowIfDisposed();
            LiteRtException.CheckStatus(
                Native.LiteRtAddOpaqueOptions(Handle, opaqueOptions));
            return this;
        }

        /// <summary>オプションが有効（Dispose 済みでない）かどうか。</summary>
        public bool IsValid => !_disposed && Handle != IntPtr.Zero;

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LiteRtOptions));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (Handle != IntPtr.Zero)
            {
                Native.LiteRtDestroyOptions(Handle);
                Handle = IntPtr.Zero;
            }
        }
    }
}
