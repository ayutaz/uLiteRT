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
