// LiteRT Unity バインディング — テンソルバッファ管理

using System;
using System.Runtime.InteropServices;

namespace LiteRT
{
    /// <summary>
    /// テンソルバッファ。入出力データの読み書きに使用する。
    /// Lock/Unlock でホストメモリにアクセスし、推論データの受け渡しを行う。
    /// 使用後は必ず Dispose すること。CompiledModel より先に Dispose すること。
    /// </summary>
    public sealed class LiteRtTensorBuffer : IDisposable
    {
        internal IntPtr Handle { get; private set; }
        private bool _disposed;

        private LiteRtTensorBuffer(IntPtr handle)
        {
            Handle = handle;
        }

        /// <summary>
        /// LiteRT 管理のテンソルバッファを作成する（推奨）。
        /// アライメント要件は LiteRT 側で自動的に満たされる。
        /// </summary>
        /// <param name="environment">LiteRT 環境。</param>
        /// <param name="bufferType">バッファの種別。</param>
        /// <param name="tensorType">テンソルの型情報。</param>
        /// <param name="bufferSize">バッファサイズ（バイト）。</param>
        public static LiteRtTensorBuffer CreateManaged(
            LiteRtEnvironment environment,
            LiteRtTensorBufferType bufferType,
            ref LiteRtRankedTensorType tensorType,
            UIntPtr bufferSize)
        {
            if (environment == null) throw new ArgumentNullException(nameof(environment));
            environment.ThrowIfDisposed();

            LiteRtException.CheckStatus(
                Native.LiteRtCreateManagedTensorBuffer(
                    environment.Handle, bufferType, ref tensorType, bufferSize,
                    out var handle));
            return new LiteRtTensorBuffer(handle);
        }

        /// <summary>
        /// CompiledModel のバッファ要件から入力テンソルバッファを作成するヘルパー。
        /// </summary>
        public static LiteRtTensorBuffer CreateFromRequirements(
            LiteRtEnvironment environment,
            LiteRtCompiledModel compiledModel,
            LiteRtModel model,
            int tensorIndex,
            bool isInput,
            int signatureIndex = 0)
        {
            IntPtr reqs = isInput
                ? compiledModel.GetInputBufferRequirements(tensorIndex, signatureIndex)
                : compiledModel.GetOutputBufferRequirements(tensorIndex, signatureIndex);

            var bufferType = LiteRtCompiledModel.GetBufferType(reqs);
            var bufferSize = LiteRtCompiledModel.GetBufferSize(reqs);

            var tensorType = isInput
                ? model.GetInputTensorType(tensorIndex, signatureIndex)
                : model.GetOutputTensorType(tensorIndex, signatureIndex);

            return CreateManaged(environment, bufferType, ref tensorType, bufferSize);
        }

        /// <summary>
        /// バッファをロックしてホストメモリポインタを取得する。
        /// 使用後は必ず Unlock を呼ぶこと。
        /// </summary>
        /// <param name="mode">ロックモード（Read/Write/ReadWrite）。</param>
        /// <returns>ホストメモリへのポインタ。</returns>
        public IntPtr Lock(LiteRtTensorBufferLockMode mode)
        {
            ThrowIfDisposed();
            LiteRtException.CheckStatus(
                Native.LiteRtLockTensorBuffer(Handle, out var hostPtr, mode));
            return hostPtr;
        }

        /// <summary>
        /// バッファのロックを解除する。
        /// </summary>
        public void Unlock()
        {
            ThrowIfDisposed();
            LiteRtException.CheckStatus(
                Native.LiteRtUnlockTensorBuffer(Handle));
        }

        /// <summary>
        /// float 配列をバッファに書き込む。
        /// </summary>
        public void WriteFloat(float[] data)
        {
            ThrowIfDisposed();
            if (data == null) throw new ArgumentNullException(nameof(data));

            IntPtr hostPtr = Lock(LiteRtTensorBufferLockMode.Write);
            try
            {
                Marshal.Copy(data, 0, hostPtr, data.Length);
            }
            finally
            {
                Unlock();
            }
        }

        /// <summary>
        /// バッファから float 配列を読み取る。
        /// </summary>
        public float[] ReadFloat()
        {
            ThrowIfDisposed();

            LiteRtException.CheckStatus(
                Native.LiteRtGetTensorBufferPackedSize(Handle, out var packedSize));
            int floatCount = (int)(ulong)packedSize / sizeof(float);

            IntPtr hostPtr = Lock(LiteRtTensorBufferLockMode.Read);
            try
            {
                var result = new float[floatCount];
                Marshal.Copy(hostPtr, result, 0, floatCount);
                return result;
            }
            finally
            {
                Unlock();
            }
        }

        /// <summary>
        /// byte 配列をバッファに書き込む。
        /// </summary>
        public void WriteBytes(byte[] data)
        {
            ThrowIfDisposed();
            if (data == null) throw new ArgumentNullException(nameof(data));

            IntPtr hostPtr = Lock(LiteRtTensorBufferLockMode.Write);
            try
            {
                Marshal.Copy(data, 0, hostPtr, data.Length);
            }
            finally
            {
                Unlock();
            }
        }

        /// <summary>
        /// バッファから byte 配列を読み取る。
        /// </summary>
        public byte[] ReadBytes()
        {
            ThrowIfDisposed();

            LiteRtException.CheckStatus(
                Native.LiteRtGetTensorBufferPackedSize(Handle, out var packedSize));
            int byteCount = (int)(ulong)packedSize;

            IntPtr hostPtr = Lock(LiteRtTensorBufferLockMode.Read);
            try
            {
                var result = new byte[byteCount];
                Marshal.Copy(hostPtr, result, 0, byteCount);
                return result;
            }
            finally
            {
                Unlock();
            }
        }

        /// <summary>バッファの種別を取得する。</summary>
        public LiteRtTensorBufferType BufferType
        {
            get
            {
                ThrowIfDisposed();
                LiteRtException.CheckStatus(
                    Native.LiteRtGetTensorBufferType(Handle, out var type));
                return type;
            }
        }

        /// <summary>バッファのパックサイズ（バイト）を取得する。</summary>
        public int PackedSize
        {
            get
            {
                ThrowIfDisposed();
                LiteRtException.CheckStatus(
                    Native.LiteRtGetTensorBufferPackedSize(Handle, out var size));
                return (int)(ulong)size;
            }
        }

        /// <summary>バッファが有効（Dispose 済みでない）かどうか。</summary>
        public bool IsValid => !_disposed && Handle != IntPtr.Zero;

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LiteRtTensorBuffer));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (Handle != IntPtr.Zero)
            {
                Native.LiteRtDestroyTensorBuffer(Handle);
                Handle = IntPtr.Zero;
            }
        }
    }
}
