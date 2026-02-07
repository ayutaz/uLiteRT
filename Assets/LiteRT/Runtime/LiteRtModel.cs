// LiteRT Unity バインディング — モデル読み込み

using System;
using System.Runtime.InteropServices;

namespace LiteRT
{
    /// <summary>
    /// LiteRT モデル。ファイルまたはバイト配列から読み込み、シグネチャ情報を取得できる。
    /// 使用後は必ず Dispose すること。
    /// </summary>
    public sealed class LiteRtModel : IDisposable
    {
        internal IntPtr Handle { get; private set; }
        private bool _disposed;

        // FromBuffer で使用: モデルの寿命中バッファを pin する
        private GCHandle _pinnedBuffer;

        private LiteRtModel(IntPtr handle)
        {
            Handle = handle;
        }

        /// <summary>
        /// ファイルパスからモデルを読み込む。
        /// </summary>
        public static LiteRtModel FromFile(string path)
        {
            LiteRtException.CheckStatus(
                Native.LiteRtCreateModelFromFile(path, out var handle));
            return new LiteRtModel(handle);
        }

        /// <summary>
        /// バイト配列からモデルを読み込む。
        /// 配列はモデルの寿命中 pin されるため、Dispose するまで GC 回収されない。
        /// </summary>
        public static LiteRtModel FromBuffer(byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (buffer.Length == 0) throw new ArgumentException("バッファが空です。", nameof(buffer));

            var pinned = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                IntPtr ptr = pinned.AddrOfPinnedObject();
                LiteRtException.CheckStatus(
                    Native.LiteRtCreateModelFromBuffer(ptr, (UIntPtr)buffer.Length, out var handle));

                var model = new LiteRtModel(handle);
                model._pinnedBuffer = pinned;
                return model;
            }
            catch
            {
                pinned.Free();
                throw;
            }
        }

        /// <summary>シグネチャ数を取得する。</summary>
        public int GetNumSignatures()
        {
            ThrowIfDisposed();
            LiteRtException.CheckStatus(
                Native.LiteRtGetNumModelSignatures(Handle, out var count));
            return (int)(ulong)count;
        }

        /// <summary>指定インデックスのシグネチャハンドルを取得する（Model が所有）。</summary>
        internal IntPtr GetSignature(int index)
        {
            ThrowIfDisposed();
            LiteRtException.CheckStatus(
                Native.LiteRtGetModelSignature(Handle, (UIntPtr)index, out var sig));
            return sig;
        }

        /// <summary>シグネチャのキー文字列を取得する。</summary>
        public string GetSignatureKey(int signatureIndex)
        {
            var sig = GetSignature(signatureIndex);
            LiteRtException.CheckStatus(
                Native.LiteRtGetSignatureKey(sig, out var keyPtr));
            return Marshal.PtrToStringAnsi(keyPtr);
        }

        /// <summary>指定シグネチャの入力テンソル数を取得する。</summary>
        public int GetNumInputs(int signatureIndex = 0)
        {
            var sig = GetSignature(signatureIndex);
            LiteRtException.CheckStatus(
                Native.LiteRtGetNumSignatureInputs(sig, out var count));
            return (int)(ulong)count;
        }

        /// <summary>指定シグネチャの出力テンソル数を取得する。</summary>
        public int GetNumOutputs(int signatureIndex = 0)
        {
            var sig = GetSignature(signatureIndex);
            LiteRtException.CheckStatus(
                Native.LiteRtGetNumSignatureOutputs(sig, out var count));
            return (int)(ulong)count;
        }

        /// <summary>入力テンソルの名前を取得する。</summary>
        public string GetInputName(int inputIndex, int signatureIndex = 0)
        {
            var sig = GetSignature(signatureIndex);
            LiteRtException.CheckStatus(
                Native.LiteRtGetSignatureInputName(sig, (UIntPtr)inputIndex, out var namePtr));
            return Marshal.PtrToStringAnsi(namePtr);
        }

        /// <summary>出力テンソルの名前を取得する。</summary>
        public string GetOutputName(int outputIndex, int signatureIndex = 0)
        {
            var sig = GetSignature(signatureIndex);
            LiteRtException.CheckStatus(
                Native.LiteRtGetSignatureOutputName(sig, (UIntPtr)outputIndex, out var namePtr));
            return Marshal.PtrToStringAnsi(namePtr);
        }

        /// <summary>入力テンソルのランク付き型情報を取得する。</summary>
        public LiteRtRankedTensorType GetInputTensorType(int inputIndex, int signatureIndex = 0)
        {
            var sig = GetSignature(signatureIndex);
            LiteRtException.CheckStatus(
                Native.LiteRtGetSignatureInputTensorByIndex(sig, (UIntPtr)inputIndex, out var tensor));
            LiteRtException.CheckStatus(
                Native.LiteRtGetRankedTensorType(tensor, out var tensorType));
            return tensorType;
        }

        /// <summary>出力テンソルのランク付き型情報を取得する。</summary>
        public LiteRtRankedTensorType GetOutputTensorType(int outputIndex, int signatureIndex = 0)
        {
            var sig = GetSignature(signatureIndex);
            LiteRtException.CheckStatus(
                Native.LiteRtGetSignatureOutputTensorByIndex(sig, (UIntPtr)outputIndex, out var tensor));
            LiteRtException.CheckStatus(
                Native.LiteRtGetRankedTensorType(tensor, out var tensorType));
            return tensorType;
        }

        /// <summary>モデルが有効（Dispose 済みでない）かどうか。</summary>
        public bool IsValid => !_disposed && Handle != IntPtr.Zero;

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LiteRtModel));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (Handle != IntPtr.Zero)
            {
                Native.LiteRtDestroyModel(Handle);
                Handle = IntPtr.Zero;
            }

            if (_pinnedBuffer.IsAllocated)
                _pinnedBuffer.Free();
        }
    }
}
