// LiteRT Unity バインディング — P/Invoke 宣言
// LiteRT 2.x CompiledModel C API に対応

using System;
using System.Runtime.InteropServices;

namespace LiteRT
{
    /// <summary>
    /// LiteRT ネイティブ C API の P/Invoke 宣言。
    /// Android では "LiteRt"、Windows/macOS では "libLiteRt" をロードする。
    /// </summary>
    public static class Native
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private const string LibName = "LiteRt";
#else
        private const string LibName = "libLiteRt";
#endif

        // =====================================================================
        // Environment
        // =====================================================================

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtCreateEnvironment(
            int numOptions,
            IntPtr options, // LiteRtEnvOption* (通常は IntPtr.Zero)
            out IntPtr environment);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void LiteRtDestroyEnvironment(IntPtr environment);

        // =====================================================================
        // Model
        // =====================================================================

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern LiteRtStatus LiteRtCreateModelFromFile(
            [MarshalAs(UnmanagedType.LPStr)] string filename,
            out IntPtr model);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtCreateModelFromBuffer(
            IntPtr buffer,
            UIntPtr bufferSize, // size_t
            out IntPtr model);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void LiteRtDestroyModel(IntPtr model);

        // =====================================================================
        // Signature
        // =====================================================================

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtGetNumModelSignatures(
            IntPtr model,
            out UIntPtr numSignatures);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtGetModelSignature(
            IntPtr model,
            UIntPtr signatureIndex,
            out IntPtr signature);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtGetSignatureKey(
            IntPtr signature,
            out IntPtr signatureKey); // const char* → Marshal.PtrToStringAnsi()

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtGetNumSignatureInputs(
            IntPtr signature,
            out UIntPtr numInputs);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtGetNumSignatureOutputs(
            IntPtr signature,
            out UIntPtr numOutputs);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtGetSignatureInputName(
            IntPtr signature,
            UIntPtr inputIndex,
            out IntPtr inputName); // const char*

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtGetSignatureOutputName(
            IntPtr signature,
            UIntPtr outputIndex,
            out IntPtr outputName); // const char*

        // =====================================================================
        // Tensor (モデルのシグネチャ内テンソル情報)
        // =====================================================================

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtGetSignatureInputTensorByIndex(
            IntPtr signature,
            UIntPtr inputIndex,
            out IntPtr tensor);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtGetSignatureOutputTensorByIndex(
            IntPtr signature,
            UIntPtr outputIndex,
            out IntPtr tensor);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtGetRankedTensorType(
            IntPtr tensor,
            out LiteRtRankedTensorType rankedTensorType);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtGetTensorTypeId(
            IntPtr tensor,
            out LiteRtTensorTypeId typeId);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtGetQuantizationTypeId(
            IntPtr tensor,
            out LiteRtQuantizationTypeId quantizationTypeId);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtGetQuantizationPerTensor(
            IntPtr tensor,
            out LiteRtQuantizationPerTensor perTensor);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtGetQuantizationPerChannel(
            IntPtr tensor,
            out LiteRtQuantizationPerChannel perChannel);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtGetTensorName(
            IntPtr tensor,
            out IntPtr name); // const char*

        // =====================================================================
        // Options
        // =====================================================================

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtCreateOptions(out IntPtr options);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void LiteRtDestroyOptions(IntPtr options);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtSetOptionsHardwareAccelerators(
            IntPtr options,
            int accelerators); // LiteRtHwAccelerators のビットフラグ

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtAddOpaqueOptions(
            IntPtr options,
            IntPtr opaqueOptions);

        // =====================================================================
        // CompiledModel
        // =====================================================================

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtCreateCompiledModel(
            IntPtr environment,
            IntPtr model,
            IntPtr options,
            out IntPtr compiledModel);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void LiteRtDestroyCompiledModel(IntPtr compiledModel);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtRunCompiledModel(
            IntPtr compiledModel,
            UIntPtr signatureIndex,
            UIntPtr numInputBuffers,
            [In] IntPtr[] inputBuffers,
            UIntPtr numOutputBuffers,
            [In] IntPtr[] outputBuffers);

        // =====================================================================
        // BufferRequirements
        // =====================================================================

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtGetCompiledModelInputBufferRequirements(
            IntPtr compiledModel,
            UIntPtr signatureIndex,
            UIntPtr inputIndex,
            out IntPtr bufferRequirements);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtGetCompiledModelOutputBufferRequirements(
            IntPtr compiledModel,
            UIntPtr signatureIndex,
            UIntPtr outputIndex,
            out IntPtr bufferRequirements);

        // BufferRequirements は CompiledModel が所有するため Destroy 不要

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtGetTensorBufferRequirementsBufferSize(
            IntPtr bufferRequirements,
            out UIntPtr bufferSize);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtGetTensorBufferRequirementsNumSupportedTensorBufferTypes(
            IntPtr bufferRequirements,
            out UIntPtr numTypes);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtGetTensorBufferRequirementsSupportedTensorBufferType(
            IntPtr bufferRequirements,
            UIntPtr typeIndex,
            out LiteRtTensorBufferType bufferType);

        // =====================================================================
        // TensorBuffer
        // =====================================================================

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtCreateManagedTensorBuffer(
            IntPtr environment,
            LiteRtTensorBufferType bufferType,
            ref LiteRtRankedTensorType tensorType,
            UIntPtr bufferSize,
            out IntPtr tensorBuffer);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void LiteRtDestroyTensorBuffer(IntPtr tensorBuffer);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtLockTensorBuffer(
            IntPtr tensorBuffer,
            out IntPtr hostMemoryPtr,
            LiteRtTensorBufferLockMode lockMode);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtUnlockTensorBuffer(IntPtr tensorBuffer);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtGetTensorBufferType(
            IntPtr tensorBuffer,
            out LiteRtTensorBufferType bufferType);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtGetTensorBufferPackedSize(
            IntPtr tensorBuffer,
            out UIntPtr packedSize);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtGetTensorBufferOffset(
            IntPtr tensorBuffer,
            out UIntPtr offset);

        // =====================================================================
        // 動的テンソルリサイズ
        // =====================================================================

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtCompiledModelResizeInputTensor(
            IntPtr compiledModel,
            UIntPtr signatureIndex,
            UIntPtr inputIndex,
            [In] int[] newDimensions,
            UIntPtr numDimensions);

        // =====================================================================
        // 非同期実行
        // =====================================================================

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtRunCompiledModelAsync(
            IntPtr compiledModel,
            UIntPtr signatureIndex,
            UIntPtr numInputBuffers,
            [In] IntPtr[] inputBuffers,
            UIntPtr numOutputBuffers,
            [In] IntPtr[] outputBuffers,
            [MarshalAs(UnmanagedType.I1)] out bool asyncExecuted);

        // =====================================================================
        // Event (非同期実行の同期)
        // =====================================================================

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtHasTensorBufferEvent(
            IntPtr tensorBuffer,
            [MarshalAs(UnmanagedType.I1)] out bool hasEvent);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtGetTensorBufferEvent(
            IntPtr tensorBuffer,
            out IntPtr eventHandle);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtIsEventSignaled(
            IntPtr eventHandle,
            [MarshalAs(UnmanagedType.I1)] out bool signaled);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtWaitEvent(
            IntPtr eventHandle,
            long timeoutMs); // -1 で無期限待機

        // =====================================================================
        // Utility
        // =====================================================================

        /// <summary>
        /// ステータスコードに対応する文字列を返す。
        /// 戻り値は静的文字列（free 不要）。Marshal.PtrToStringAnsi() で変換すること。
        /// </summary>
        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr LiteRtGetStatusString(LiteRtStatus status);
    }
}
