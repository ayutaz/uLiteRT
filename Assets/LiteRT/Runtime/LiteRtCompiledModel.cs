// LiteRT Unity バインディング — コンパイル済みモデル（推論実行）

using System;
using System.Runtime.InteropServices;

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

        // キャンセルコールバックを GC から保護するために保持
        private Native.LiteRtCancellationCallback _cancellationCallback;

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

        /// <summary>
        /// 非同期推論を実行する。
        /// </summary>
        /// <param name="inputBuffers">入力テンソルバッファのハンドル配列。</param>
        /// <param name="outputBuffers">出力テンソルバッファのハンドル配列。</param>
        /// <param name="asyncExecuted">非同期で実行された場合 true。</param>
        /// <param name="signatureIndex">シグネチャインデックス（通常 0）。</param>
        public void RunAsync(IntPtr[] inputBuffers, IntPtr[] outputBuffers,
            out bool asyncExecuted, int signatureIndex = 0)
        {
            ThrowIfDisposed();
            if (inputBuffers == null) throw new ArgumentNullException(nameof(inputBuffers));
            if (outputBuffers == null) throw new ArgumentNullException(nameof(outputBuffers));

            LiteRtException.CheckStatus(
                Native.LiteRtRunCompiledModelAsync(
                    Handle,
                    (UIntPtr)signatureIndex,
                    (UIntPtr)inputBuffers.Length, inputBuffers,
                    (UIntPtr)outputBuffers.Length, outputBuffers,
                    out asyncExecuted));
        }

        /// <summary>
        /// LiteRtTensorBuffer ラッパーの配列で非同期推論を実行する。
        /// </summary>
        public void RunAsync(LiteRtTensorBuffer[] inputBuffers, LiteRtTensorBuffer[] outputBuffers,
            out bool asyncExecuted, int signatureIndex = 0)
        {
            var inHandles = new IntPtr[inputBuffers.Length];
            for (int i = 0; i < inputBuffers.Length; i++)
                inHandles[i] = inputBuffers[i].Handle;

            var outHandles = new IntPtr[outputBuffers.Length];
            for (int i = 0; i < outputBuffers.Length; i++)
                outHandles[i] = outputBuffers[i].Handle;

            RunAsync(inHandles, outHandles, out asyncExecuted, signatureIndex);
        }

        /// <summary>
        /// 入力テンソルをリサイズする（strict モード）。
        /// 動的次元（-1）が指定されている次元のみ変更可能。
        /// </summary>
        public void ResizeInputTensor(int inputIndex, int[] dims, int signatureIndex = 0)
        {
            ThrowIfDisposed();
            if (dims == null) throw new ArgumentNullException(nameof(dims));

            LiteRtException.CheckStatus(
                Native.LiteRtCompiledModelResizeInputTensor(
                    Handle, (UIntPtr)signatureIndex, (UIntPtr)inputIndex,
                    dims, (UIntPtr)dims.Length));
        }

        /// <summary>
        /// 入力テンソルをリサイズする（non-strict モード）。
        /// 任意の次元を変更可能。
        /// </summary>
        public void ResizeInputTensorNonStrict(int inputIndex, int[] dims, int signatureIndex = 0)
        {
            ThrowIfDisposed();
            if (dims == null) throw new ArgumentNullException(nameof(dims));

            LiteRtException.CheckStatus(
                Native.LiteRtCompiledModelResizeInputTensorNonStrict(
                    Handle, (UIntPtr)signatureIndex, (UIntPtr)inputIndex,
                    dims, (UIntPtr)dims.Length));
        }

        /// <summary>
        /// 入力テンソルのレイアウトを取得する。
        /// </summary>
        public LiteRtLayout GetInputTensorLayout(int inputIndex, int signatureIndex = 0)
        {
            ThrowIfDisposed();
            LiteRtException.CheckStatus(
                Native.LiteRtGetCompiledModelInputTensorLayout(
                    Handle, (UIntPtr)signatureIndex, (UIntPtr)inputIndex, out var layout));
            return layout;
        }

        /// <summary>
        /// 全出力テンソルのレイアウトを一括取得する。
        /// </summary>
        /// <param name="numOutputs">出力テンソル数。</param>
        /// <param name="updateAllocation">アロケーションを更新するかどうか。</param>
        /// <param name="signatureIndex">シグネチャインデックス。</param>
        public LiteRtLayout[] GetOutputTensorLayouts(int numOutputs, bool updateAllocation = false,
            int signatureIndex = 0)
        {
            ThrowIfDisposed();
            var layouts = new LiteRtLayout[numOutputs];
            LiteRtException.CheckStatus(
                Native.LiteRtGetCompiledModelOutputTensorLayouts(
                    Handle, (UIntPtr)signatureIndex, (UIntPtr)numOutputs,
                    layouts, updateAllocation));
            return layouts;
        }

        /// <summary>モデルが完全にアクセラレートされているかどうか。</summary>
        public bool IsFullyAccelerated
        {
            get
            {
                ThrowIfDisposed();
                LiteRtException.CheckStatus(
                    Native.LiteRtCompiledModelIsFullyAccelerated(Handle, out var result));
                return result;
            }
        }

        /// <summary>
        /// 推論のキャンセル関数を設定する。
        /// コールバックが true を返すと推論がキャンセルされる。
        /// </summary>
        /// <param name="callback">キャンセル判定コールバック。</param>
        /// <param name="userData">コールバックに渡すユーザーデータ。</param>
        public void SetCancellationFunction(Native.LiteRtCancellationCallback callback,
            IntPtr userData = default)
        {
            ThrowIfDisposed();
            // デリゲートを GC から保護
            _cancellationCallback = callback;
            LiteRtException.CheckStatus(
                Native.LiteRtSetCompiledModelCancellationFunction(Handle, userData, callback));
        }

        /// <summary>
        /// コンパイル済みモデルのエラーメッセージを取得する。
        /// </summary>
        /// <remarks>
        /// 返された文字列のネイティブメモリは free() が必要だが、
        /// C# から C の free を呼ぶのは困難なため、メモリリークを許容する（ガイド§7.4 準拠）。
        /// </remarks>
        public string GetErrorMessages()
        {
            ThrowIfDisposed();
            LiteRtException.CheckStatus(
                Native.LiteRtCompiledModelGetErrorMessages(Handle, out var msgPtr));
            if (msgPtr == IntPtr.Zero) return null;
            return Marshal.PtrToStringAnsi(msgPtr);
        }

        /// <summary>
        /// コンパイル済みモデルのエラーをクリアする。
        /// </summary>
        public void ClearErrors()
        {
            ThrowIfDisposed();
            LiteRtException.CheckStatus(
                Native.LiteRtCompiledModelClearErrors(Handle));
        }

        /// <summary>
        /// コンパイル済みモデルのプロファイラハンドルを取得する（CompiledModel が所有）。
        /// </summary>
        public IntPtr GetProfiler()
        {
            ThrowIfDisposed();
            LiteRtException.CheckStatus(
                Native.LiteRtCompiledModelGetProfiler(Handle, out var profiler));
            return profiler;
        }

        /// <summary>プロファイラを開始する。</summary>
        public static void StartProfiler(IntPtr profiler)
        {
            LiteRtException.CheckStatus(Native.LiteRtStartProfiler(profiler));
        }

        /// <summary>プロファイラを停止する。</summary>
        public static void StopProfiler(IntPtr profiler)
        {
            LiteRtException.CheckStatus(Native.LiteRtStopProfiler(profiler));
        }

        /// <summary>プロファイラをリセットする。</summary>
        public static void ResetProfiler(IntPtr profiler)
        {
            LiteRtException.CheckStatus(Native.LiteRtResetProfiler(profiler));
        }

        /// <summary>プロファイラのイベント数を取得する。</summary>
        public static int GetNumProfilerEvents(IntPtr profiler)
        {
            LiteRtException.CheckStatus(
                Native.LiteRtGetNumProfilerEvents(profiler, out var count));
            return (int)(ulong)count;
        }

        /// <summary>
        /// バッファ要件からアライメントを取得する。
        /// </summary>
        public static UIntPtr GetBufferAlignment(IntPtr bufferRequirements)
        {
            LiteRtException.CheckStatus(
                Native.LiteRtGetTensorBufferRequirementsAlignment(
                    bufferRequirements, out var alignment));
            return alignment;
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
