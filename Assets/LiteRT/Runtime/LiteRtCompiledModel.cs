// LiteRT Unity バインディング — コンパイル済みモデル（推論実行）

using System;

namespace LiteRT
{
    /// <summary>
    /// コンパイル済みモデル。推論の実行とバッファ要件の取得を行う。
    /// 使用後は必ず Dispose すること。Model より先に Dispose すること。
    /// </summary>
    public sealed class LiteRtCompiledModel : IDisposable
    {
        internal IntPtr Handle { get; private set; }
        private bool _disposed;

        /// <summary>
        /// モデルをコンパイルして推論可能な状態にする。
        /// </summary>
        /// <param name="environment">LiteRT 環境。</param>
        /// <param name="model">コンパイル対象のモデル。</param>
        /// <param name="options">コンパイルオプション。</param>
        public LiteRtCompiledModel(LiteRtEnvironment environment, LiteRtModel model, LiteRtOptions options)
        {
            if (environment == null) throw new ArgumentNullException(nameof(environment));
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (options == null) throw new ArgumentNullException(nameof(options));

            environment.ThrowIfDisposed();

            LiteRtException.CheckStatus(
                Native.LiteRtCreateCompiledModel(
                    environment.Handle, model.Handle, options.Handle, out var handle));
            Handle = handle;
        }

        /// <summary>
        /// 同期推論を実行する。
        /// </summary>
        /// <param name="inputBuffers">入力テンソルバッファのハンドル配列。</param>
        /// <param name="outputBuffers">出力テンソルバッファのハンドル配列。</param>
        /// <param name="signatureIndex">シグネチャインデックス（通常 0）。</param>
        public void Run(IntPtr[] inputBuffers, IntPtr[] outputBuffers, int signatureIndex = 0)
        {
            ThrowIfDisposed();
            if (inputBuffers == null) throw new ArgumentNullException(nameof(inputBuffers));
            if (outputBuffers == null) throw new ArgumentNullException(nameof(outputBuffers));

            LiteRtException.CheckStatus(
                Native.LiteRtRunCompiledModel(
                    Handle,
                    (UIntPtr)signatureIndex,
                    (UIntPtr)inputBuffers.Length, inputBuffers,
                    (UIntPtr)outputBuffers.Length, outputBuffers));
        }

        /// <summary>
        /// LiteRtTensorBuffer ラッパーの配列で同期推論を実行する。
        /// </summary>
        public void Run(LiteRtTensorBuffer[] inputBuffers, LiteRtTensorBuffer[] outputBuffers,
            int signatureIndex = 0)
        {
            var inHandles = new IntPtr[inputBuffers.Length];
            for (int i = 0; i < inputBuffers.Length; i++)
                inHandles[i] = inputBuffers[i].Handle;

            var outHandles = new IntPtr[outputBuffers.Length];
            for (int i = 0; i < outputBuffers.Length; i++)
                outHandles[i] = outputBuffers[i].Handle;

            Run(inHandles, outHandles, signatureIndex);
        }

        /// <summary>
        /// 入力バッファ要件を取得する（CompiledModel が所有、Destroy 不要）。
        /// </summary>
        internal IntPtr GetInputBufferRequirements(int inputIndex, int signatureIndex = 0)
        {
            ThrowIfDisposed();
            LiteRtException.CheckStatus(
                Native.LiteRtGetCompiledModelInputBufferRequirements(
                    Handle, (UIntPtr)signatureIndex, (UIntPtr)inputIndex, out var reqs));
            return reqs;
        }

        /// <summary>
        /// 出力バッファ要件を取得する（CompiledModel が所有、Destroy 不要）。
        /// </summary>
        internal IntPtr GetOutputBufferRequirements(int outputIndex, int signatureIndex = 0)
        {
            ThrowIfDisposed();
            LiteRtException.CheckStatus(
                Native.LiteRtGetCompiledModelOutputBufferRequirements(
                    Handle, (UIntPtr)signatureIndex, (UIntPtr)outputIndex, out var reqs));
            return reqs;
        }

        /// <summary>
        /// バッファ要件から推奨されるバッファ型を取得する。
        /// </summary>
        public static LiteRtTensorBufferType GetBufferType(IntPtr bufferRequirements, int typeIndex = 0)
        {
            LiteRtException.CheckStatus(
                Native.LiteRtGetTensorBufferRequirementsSupportedTensorBufferType(
                    bufferRequirements, (UIntPtr)typeIndex, out var bufferType));
            return bufferType;
        }

        /// <summary>
        /// バッファ要件から必要なバッファサイズを取得する。
        /// </summary>
        public static UIntPtr GetBufferSize(IntPtr bufferRequirements)
        {
            LiteRtException.CheckStatus(
                Native.LiteRtGetTensorBufferRequirementsBufferSize(
                    bufferRequirements, out var size));
            return size;
        }

        /// <summary>コンパイル済みモデルが有効（Dispose 済みでない）かどうか。</summary>
        public bool IsValid => !_disposed && Handle != IntPtr.Zero;

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LiteRtCompiledModel));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (Handle != IntPtr.Zero)
            {
                Native.LiteRtDestroyCompiledModel(Handle);
                Handle = IntPtr.Zero;
            }
        }
    }
}
