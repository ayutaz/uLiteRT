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
        // GPU Options (§4.6)
        // =====================================================================

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtCreateGpuOptions(
            out IntPtr options);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtSetGpuOptionsGpuBackend(
            IntPtr gpuOptions,
            LiteRtGpuBackend backend);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtSetGpuAcceleratorCompilationOptionsPrecision(
            IntPtr gpuOptions,
            LiteRtDelegatePrecision precision);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtSetGpuOptionsGpuPriority(
            IntPtr gpuOptions,
            LiteRtGpuPriority priority);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtSetGpuOptionsExternalTensorsMode(
            IntPtr gpuOptions,
            [MarshalAs(UnmanagedType.I1)] bool enable);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern LiteRtStatus LiteRtSetGpuAcceleratorCompilationOptionsSerializationDir(
            IntPtr gpuOptions,
            [MarshalAs(UnmanagedType.LPStr)] string serializationDir);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern LiteRtStatus LiteRtSetGpuAcceleratorCompilationOptionsModelCacheKey(
            IntPtr gpuOptions,
            [MarshalAs(UnmanagedType.LPStr)] string modelCacheKey);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtSetGpuOptionsHintFullyDelegatedToSingleDelegate(
            IntPtr gpuOptions,
            [MarshalAs(UnmanagedType.I1)] bool hint);

        // =====================================================================
        // CPU Options (§4.7)
        // =====================================================================

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtCreateCpuOptions(
            out IntPtr options);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtFindCpuOptions(
            IntPtr opaqueOptions,
            out IntPtr cpuOptions);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtSetCpuOptionsNumThread(
            IntPtr cpuOptions,
            int numThreads);

        // =====================================================================
        // Runtime Options (§4.8)
        // =====================================================================

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtCreateRuntimeOptions(
            out IntPtr options);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtFindRuntimeOptions(
            IntPtr opaqueOptions,
            out IntPtr runtimeOptions);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtSetRuntimeOptionsEnableProfiling(
            IntPtr runtimeOptions,
            [MarshalAs(UnmanagedType.I1)] bool enableProfiling);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtSetRuntimeOptionsErrorReporterMode(
            IntPtr runtimeOptions,
            LiteRtErrorReporterMode errorReporterMode);

        // =====================================================================
        // CompiledModel 追加 (§4.9)
        // =====================================================================

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtCompiledModelIsFullyAccelerated(
            IntPtr compiledModel,
            [MarshalAs(UnmanagedType.I1)] out bool fullyAccelerated);

        /// <summary>
        /// キャンセルコールバック。true を返すと推論がキャンセルされる。
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public delegate bool LiteRtCancellationCallback(IntPtr data);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtSetCompiledModelCancellationFunction(
            IntPtr compiledModel,
            IntPtr data,
            LiteRtCancellationCallback callback);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtCompiledModelResizeInputTensorNonStrict(
            IntPtr compiledModel,
            UIntPtr signatureIndex,
            UIntPtr inputIndex,
            [In] int[] newDimensions,
            UIntPtr numDimensions);

        // =====================================================================
        // BufferRequirements 追加 (§4.10)
        // =====================================================================

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtGetTensorBufferRequirementsAlignment(
            IntPtr bufferRequirements,
            out UIntPtr alignment);

        // =====================================================================
        // Tensor Layout (§4.11)
        // =====================================================================

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtGetCompiledModelInputTensorLayout(
            IntPtr compiledModel,
            UIntPtr signatureIndex,
            UIntPtr inputIndex,
            out LiteRtLayout layout);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtGetCompiledModelOutputTensorLayouts(
            IntPtr compiledModel,
            UIntPtr signatureIndex,
            UIntPtr numLayouts,
            [In, Out] LiteRtLayout[] layouts,
            [MarshalAs(UnmanagedType.I1)] bool updateAllocation);

        // =====================================================================
        // TensorBuffer 追加 (§4.12)
        // =====================================================================

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtCreateManagedTensorBufferFromRequirements(
            IntPtr environment,
            ref LiteRtRankedTensorType tensorType,
            IntPtr requirements,
            out IntPtr buffer);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtCreateTensorBufferFromHostMemory(
            ref LiteRtRankedTensorType tensorType,
            IntPtr hostBufferAddr,
            UIntPtr hostBufferSize,
            IntPtr deallocator,
            out IntPtr buffer);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtGetTensorBufferSize(
            IntPtr tensorBuffer,
            out UIntPtr size);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtGetTensorBufferTensorType(
            IntPtr tensorBuffer,
            out LiteRtRankedTensorType tensorType);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtDuplicateTensorBuffer(
            IntPtr tensorBuffer,
            out IntPtr duplicatedBuffer);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtClearTensorBuffer(IntPtr buffer);

        // =====================================================================
        // OpenGL (§4.13)
        // =====================================================================

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtCreateTensorBufferFromGlBuffer(
            IntPtr environment,
            ref LiteRtRankedTensorType tensorType,
            uint target,
            uint id,
            UIntPtr sizeBytes,
            UIntPtr offset,
            IntPtr deallocator,
            out IntPtr buffer);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtCreateTensorBufferFromGlTexture(
            IntPtr environment,
            ref LiteRtRankedTensorType tensorType,
            uint target,
            uint id,
            uint format,
            UIntPtr sizeBytes,
            int layer,
            IntPtr deallocator,
            out IntPtr buffer);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtGetTensorBufferGlBuffer(
            IntPtr tensorBuffer,
            out uint target,
            out uint id,
            out UIntPtr sizeBytes,
            out UIntPtr offset);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtGetTensorBufferGlTexture(
            IntPtr tensorBuffer,
            out uint target,
            out uint id,
            out uint format,
            out UIntPtr sizeBytes,
            out int layer);

        // =====================================================================
        // Event 追加 (§4.14)
        // =====================================================================

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void LiteRtDestroyEvent(IntPtr eventHandle);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtSetTensorBufferEvent(
            IntPtr tensorBuffer,
            IntPtr eventHandle);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtClearTensorBufferEvent(
            IntPtr tensorBuffer);

        // =====================================================================
        // Profiler (§4.16)
        // =====================================================================

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtCompiledModelGetProfiler(
            IntPtr compiledModel,
            out IntPtr profiler);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtStartProfiler(IntPtr profiler);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtStopProfiler(IntPtr profiler);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtResetProfiler(IntPtr profiler);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtGetNumProfilerEvents(
            IntPtr profiler,
            out UIntPtr numEvents);

        // =====================================================================
        // Error Handling 追加 (§4.17)
        // =====================================================================

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtCompiledModelGetErrorMessages(
            IntPtr compiledModel,
            out IntPtr errorMessages);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern LiteRtStatus LiteRtCompiledModelClearErrors(
            IntPtr compiledModel);

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
