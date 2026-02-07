// LiteRT Unity バインディング — 構造体定義
// LiteRT 2.x CompiledModel C API に対応

using System;
using System.Runtime.InteropServices;

namespace LiteRT
{
    /// <summary>
    /// テンソルのシェイプとストライドを表す構造体。
    /// C側ではビットフィールド (rank:7, has_strides:1) で定義されている。
    /// </summary>
    /// <remarks>
    /// C定義:
    ///   typedef struct {
    ///     unsigned int rank : 7;
    ///     bool has_strides : 1;
    ///     int32_t dimensions[8];
    ///     uint32_t strides[8];
    ///   } LiteRtLayout;
    ///
    /// ビットフィールド + パディングで先頭 4 バイトに pack される。
    /// 全体サイズは 4 + 32 + 32 = 68 バイト。
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct LiteRtLayout
    {
        private uint _rankAndFlags; // 下位7ビット=rank, 8ビット目=has_strides

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public int[] dimensions;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public uint[] strides;

        /// <summary>テンソルのランク（次元数）。</summary>
        public int Rank => (int)(_rankAndFlags & 0x7F);

        /// <summary>ストライド情報を持つかどうか。</summary>
        public bool HasStrides => (_rankAndFlags & 0x80) != 0;

        /// <summary>
        /// dimensions[0..Rank-1] を int[] として取得する。
        /// </summary>
        public int[] GetDimensions()
        {
            var dims = new int[Rank];
            Array.Copy(dimensions, dims, Rank);
            return dims;
        }

        /// <summary>
        /// strides[0..Rank-1] を uint[] として取得する。HasStrides が true のときのみ有効。
        /// </summary>
        public uint[] GetStrides()
        {
            if (!HasStrides) return Array.Empty<uint>();
            var s = new uint[Rank];
            Array.Copy(strides, s, Rank);
            return s;
        }
    }

    /// <summary>
    /// ランク付きテンソル型。要素型とレイアウト情報を持つ。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct LiteRtRankedTensorType
    {
        public LiteRtElementType elementType;
        public LiteRtLayout layout;
    }

    /// <summary>
    /// ランクなしテンソル型。要素型のみを持つ。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct LiteRtUnrankedTensorType
    {
        public LiteRtElementType elementType;
    }

    /// <summary>
    /// テンソル単位の量子化パラメータ。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct LiteRtQuantizationPerTensor
    {
        public float scale;
        public long zeroPoint; // int64_t
    }

    /// <summary>
    /// チャネル単位の量子化パラメータ。
    /// scales と zeroPoints はネイティブ側が所有するポインタ。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct LiteRtQuantizationPerChannel
    {
        public IntPtr scales;       // const float*
        public IntPtr zeroPoints;   // const int64_t*
        public int quantizedDimension;
        public UIntPtr numChannels; // size_t
    }

    /// <summary>
    /// API バージョン情報。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct LiteRtApiVersion
    {
        public int major;
        public int minor;
        public int patch;
    }
}
