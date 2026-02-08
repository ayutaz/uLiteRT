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

        ~LiteRtEnvironment() { Dispose(); }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (Handle != IntPtr.Zero)
            {
                Native.LiteRtDestroyEnvironment(Handle);
                Handle = IntPtr.Zero;
            }

            GC.SuppressFinalize(this);
        }
    }
}
