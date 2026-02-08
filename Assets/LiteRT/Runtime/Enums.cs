// LiteRT Unity バインディング — 列挙型定義
// LiteRT 2.x CompiledModel C API に対応

using System;

namespace LiteRT
{
    /// <summary>
    /// LiteRT API のステータスコード。
    /// </summary>
    public enum LiteRtStatus : int
    {
        Ok = 0,
        ErrorInvalidArgument = 1,
        ErrorMemoryAllocationFailure = 2,
        ErrorRuntimeFailure = 3,
        ErrorMissingInputTensor = 4,
        ErrorUnsupported = 5,
        ErrorNotFound = 6,
        ErrorTimeoutExpired = 7,
        ErrorWrongVersion = 8,
        ErrorUnknown = 9,
        ErrorAlreadyExists = 10,
        Cancelled = 100,
        ErrorFileIO = 500,
        ErrorInvalidFlatbuffer = 501,
        ErrorDynamicLoading = 502,
        ErrorSerialization = 503,
        ErrorCompilation = 504,
        ErrorIndexOOB = 1000,
        ErrorInvalidIrType = 1001,
        ErrorInvalidGraphInvariant = 1002,
        ErrorGraphModification = 1003,
        ErrorInvalidToolConfig = 1500,
    }

    /// <summary>
    /// テンソル要素の型。
    /// </summary>
    public enum LiteRtElementType : int
    {
        None = 0,
        Float32 = 1,
        Int32 = 2,
        UInt8 = 3,
        Int64 = 4,
        TfString = 5,
        Bool = 6,
        Int16 = 7,
        Complex64 = 8,
        Int8 = 9,
        Float16 = 10,
        Float64 = 11,
        Complex128 = 12,
        UInt64 = 13,
        TfResource = 14,
        TfVariant = 15,
        UInt32 = 16,
        UInt16 = 17,
        Int4 = 18,
        BFloat16 = 19,
        Int2 = 20,
    }

    /// <summary>
    /// ハードウェアアクセラレータの指定（ビットフラグ）。
    /// </summary>
    [Flags]
    public enum LiteRtHwAccelerators : int
    {
        None = 0,
        Cpu = 1 << 0,
        Gpu = 1 << 1,
        Npu = 1 << 2,
    }

    /// <summary>
    /// テンソルバッファの種別。
    /// </summary>
    public enum LiteRtTensorBufferType : int
    {
        Unknown = 0,
        HostMemory = 1,
        Ahwb = 2,
        Ion = 3,
        DmaBuf = 4,
        FastRpc = 5,
        GlBuffer = 6,
        GlTexture = 7,
        // OpenCL (10-19)
        OpenClBuffer = 10,
        OpenClBufferFp16 = 11,
        OpenClTexture = 12,
        OpenClTextureFp16 = 13,
        OpenClBufferPacked = 14,
        OpenClImageBuffer = 15,
        OpenClImageBufferFp16 = 16,
        // WebGPU (20-29)
        WebGpuBuffer = 20,
        WebGpuBufferFp16 = 21,
        WebGpuTexture = 22,
        WebGpuTextureFp16 = 23,
        WebGpuImageBuffer = 24,
        WebGpuImageBufferFp16 = 25,
        WebGpuBufferPacked = 26,
        // Metal (30-39)
        MetalBuffer = 30,
        MetalBufferFp16 = 31,
        MetalTexture = 32,
        MetalTextureFp16 = 33,
        MetalBufferPacked = 34,
        // Vulkan (40-49)
        VulkanBuffer = 40,
        VulkanBufferFp16 = 41,
        VulkanTexture = 42,
        VulkanTextureFp16 = 43,
        VulkanImageBuffer = 44,
        VulkanImageBufferFp16 = 45,
        VulkanBufferPacked = 46,
        // Custom (100-199)
        UserCustomBuffer = 100,
        OpenVINOTensorBuffer = 100,
        UserCustomBufferEnd = 199,
    }

    /// <summary>
    /// テンソルバッファのロックモード。
    /// </summary>
    public enum LiteRtTensorBufferLockMode : int
    {
        Read = 0,
        Write = 1,
        ReadWrite = 2,
    }

    /// <summary>
    /// テンソル型の種別。
    /// </summary>
    public enum LiteRtTensorTypeId : int
    {
        Ranked = 0,
        Unranked = 1,
    }

    /// <summary>
    /// 量子化の種別。
    /// </summary>
    public enum LiteRtQuantizationTypeId : int
    {
        None = 0,
        PerTensor = 1,
        PerChannel = 2,
        BlockWise = 3,
    }

    /// <summary>
    /// GPU バックエンドの種別。
    /// </summary>
    public enum LiteRtGpuBackend : int
    {
        Automatic = 0,
        OpenCl = 1,
        WebGpu = 2,
        OpenGl = 3,
    }

    /// <summary>
    /// GPU 実行優先度。
    /// </summary>
    public enum LiteRtGpuPriority : int
    {
        Default = 0,
        Low = 1,
        Normal = 2,
        High = 3,
    }

    /// <summary>
    /// デリゲートの計算精度。
    /// </summary>
    public enum LiteRtDelegatePrecision : int
    {
        Default = 0,
        Fp16 = 1,
        Fp32 = 2,
    }

    /// <summary>
    /// GPU 待機方式。
    /// </summary>
    public enum LiteRtGpuWaitType : int
    {
        Default = 0,
        Passive = 1,
        Active = 2,
        DoNotWait = 3,
    }

    /// <summary>
    /// エラーレポータのモード。
    /// </summary>
    public enum LiteRtErrorReporterMode : int
    {
        None = 0,
        Stderr = 1,
        Buffer = 2,
    }

    /// <summary>
    /// デリゲートバッファのストレージ種別。
    /// </summary>
    public enum LiteRtDelegateBufferStorageType : int
    {
        Default = 0,
        Buffer = 1,
        Texture2D = 2,
    }
}
