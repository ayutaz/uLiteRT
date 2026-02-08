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
        private bool _ownershipTransferred;

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

        internal void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CpuOptions));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (!_ownershipTransferred && Handle != IntPtr.Zero)
            {
                UnityEngine.Debug.LogWarning(
                    "CpuOptions: LiteRtOptions に追加されずに破棄されました。ネイティブメモリがリークします。");
            }

            Handle = IntPtr.Zero;
        }
    }
}
