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
            try { return new LiteRtTensorBuffer(handle); }
            catch { Native.LiteRtDestroyTensorBuffer(handle); throw; }
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
            int floatCount = checked((int)(ulong)packedSize) / sizeof(float);

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
            int byteCount = checked((int)(ulong)packedSize);

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
                return checked((int)(ulong)size);
            }
        }

        /// <summary>
        /// バッファ要件から LiteRT 管理のテンソルバッファを作成する（推奨）。
        /// アライメント要件が自動的に処理される。
        /// </summary>
        public static LiteRtTensorBuffer CreateFromRequirements(
            LiteRtEnvironment environment,
            ref LiteRtRankedTensorType tensorType,
            IntPtr requirements)
        {
            if (environment == null) throw new ArgumentNullException(nameof(environment));
            environment.ThrowIfDisposed();

            LiteRtException.CheckStatus(
                Native.LiteRtCreateManagedTensorBufferFromRequirements(
                    environment.Handle, ref tensorType, requirements, out var handle));
            try { return new LiteRtTensorBuffer(handle); }
            catch { Native.LiteRtDestroyTensorBuffer(handle); throw; }
        }

        /// <summary>
        /// ホストメモリからテンソルバッファを作成する。
        /// hostBuffer は 64 バイトアライメントが必須。
        /// </summary>
        public static LiteRtTensorBuffer CreateFromHostMemory(
            ref LiteRtRankedTensorType tensorType,
            IntPtr hostBuffer,
            UIntPtr size)
        {
            LiteRtException.CheckStatus(
                Native.LiteRtCreateTensorBufferFromHostMemory(
                    ref tensorType, hostBuffer, size, IntPtr.Zero, out var handle));
            try { return new LiteRtTensorBuffer(handle); }
            catch { Native.LiteRtDestroyTensorBuffer(handle); throw; }
        }

        /// <summary>テンソルの型情報を取得する。</summary>
        public LiteRtRankedTensorType TensorType
        {
            get
            {
                ThrowIfDisposed();
                LiteRtException.CheckStatus(
                    Native.LiteRtGetTensorBufferTensorType(Handle, out var tensorType));
                return tensorType;
            }
        }

        /// <summary>バッファの非パックサイズ（バイト）を取得する。</summary>
        public int Size
        {
            get
            {
                ThrowIfDisposed();
                LiteRtException.CheckStatus(
                    Native.LiteRtGetTensorBufferSize(Handle, out var size));
                return checked((int)(ulong)size);
            }
        }

        /// <summary>
        /// テンソルバッファを複製する。
        /// 参照カウントが増加され、同じネイティブバッファを指す新ラッパーを返す。
        /// 各ラッパーの Dispose() が DestroyTensorBuffer を呼び、参照カウントをデクリメントする。
        /// </summary>
        public LiteRtTensorBuffer Duplicate()
        {
            ThrowIfDisposed();
            LiteRtException.CheckStatus(
                Native.LiteRtDuplicateTensorBuffer(Handle));
            // 参照カウント増加済みなので、同じハンドルの新ラッパーを返す
            return new LiteRtTensorBuffer(Handle);
        }

        /// <summary>テンソルバッファの内容をクリアする。</summary>
        public void Clear()
        {
            ThrowIfDisposed();
            LiteRtException.CheckStatus(
                Native.LiteRtClearTensorBuffer(Handle));
        }

        /// <summary>バッファにイベントが関連付けられているかどうか。</summary>
        public bool HasEvent()
        {
            ThrowIfDisposed();
            LiteRtException.CheckStatus(
                Native.LiteRtHasTensorBufferEvent(Handle, out var hasEvent));
            return hasEvent;
        }

        /// <summary>バッファに関連付けられたイベントハンドルを取得する。</summary>
        public IntPtr GetEvent()
        {
            ThrowIfDisposed();
            LiteRtException.CheckStatus(
                Native.LiteRtGetTensorBufferEvent(Handle, out var eventHandle));
            return eventHandle;
        }

        /// <summary>バッファにイベントを設定する。バッファが所有権を取得する。</summary>
        public void SetEvent(IntPtr eventHandle)
        {
            ThrowIfDisposed();
            LiteRtException.CheckStatus(
                Native.LiteRtSetTensorBufferEvent(Handle, eventHandle));
        }

        /// <summary>バッファからイベントをクリアする。</summary>
        public void ClearEvent()
        {
            ThrowIfDisposed();
            LiteRtException.CheckStatus(
                Native.LiteRtClearTensorBufferEvent(Handle));
        }

        /// <summary>イベントがシグナル状態かどうかを確認する。</summary>
        public static bool IsEventSignaled(IntPtr eventHandle)
        {
            LiteRtException.CheckStatus(
                Native.LiteRtIsEventSignaled(eventHandle, out var signaled));
            return signaled;
        }

        /// <summary>
        /// イベントの完了を待機する。
        /// </summary>
        /// <param name="eventHandle">イベントハンドル。</param>
        /// <param name="timeoutMs">タイムアウト（ミリ秒）。-1 で無期限。</param>
        public static void WaitEvent(IntPtr eventHandle, long timeoutMs = -1)
        {
            LiteRtException.CheckStatus(
                Native.LiteRtWaitEvent(eventHandle, timeoutMs));
        }

        /// <summary>バッファが有効（Dispose 済みでない）かどうか。</summary>
        public bool IsValid => !_disposed && Handle != IntPtr.Zero;

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LiteRtTensorBuffer));
        }

        ~LiteRtTensorBuffer() { Dispose(); }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (Handle != IntPtr.Zero)
            {
                Native.LiteRtDestroyTensorBuffer(Handle);
                Handle = IntPtr.Zero;
            }

            GC.SuppressFinalize(this);
        }
    }
}
