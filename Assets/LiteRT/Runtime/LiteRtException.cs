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

// LiteRT Unity バインディング — 例外クラス

using System;
using System.Runtime.InteropServices;

namespace LiteRT
{
    /// <summary>
    /// LiteRT API がエラーステータスを返した際にスローされる例外。
    /// </summary>
    public class LiteRtException : Exception
    {
        /// <summary>ネイティブ API が返したステータスコード。</summary>
        public LiteRtStatus Status { get; }

        public LiteRtException(LiteRtStatus status, string message)
            : base(message)
        {
            Status = status;
        }

        /// <summary>
        /// ステータスが Ok でない場合に例外をスローする。
        /// 全ての P/Invoke 呼び出し後に使用すること。
        /// </summary>
        internal static void CheckStatus(LiteRtStatus status)
        {
            if (status == LiteRtStatus.Ok) return;

            string nativeMsg = null;
            try
            {
                IntPtr ptr = Native.LiteRtGetStatusString(status);
                if (ptr != IntPtr.Zero)
                    nativeMsg = Marshal.PtrToStringAnsi(ptr);
            }
            catch
            {
                // LiteRtGetStatusString 自体が失敗した場合は無視
            }

            string message = nativeMsg != null
                ? $"LiteRT error: {status} — {nativeMsg}"
                : $"LiteRT error: {status}";

            throw new LiteRtException(status, message);
        }
    }
}
