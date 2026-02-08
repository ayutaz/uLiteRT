# uLiteRT

[![Unity](https://img.shields.io/badge/Unity-6000.3+-black.svg)](https://unity.com/)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![LiteRT](https://img.shields.io/badge/LiteRT-v2.1.2-orange.svg)](https://github.com/google-ai-edge/LiteRT)

Unity C# bindings for [Google LiteRT](https://github.com/google-ai-edge/LiteRT) (formerly TensorFlow Lite) via P/Invoke.

[日本語](README_JP.md)

## Overview

uLiteRT provides C# bindings that directly call the LiteRT 2.x CompiledModel C API through P/Invoke. A single shared C# codebase works across all platforms — only the native library (.dll/.so/.dylib) differs per platform.

## Features

- **P/Invoke-based** — platform-independent C# code with per-platform native libraries
- **Hardware acceleration** — CPU, GPU, and NPU support
- **IDisposable resource management** — deterministic cleanup of native resources
- **Builder-pattern options** — GPU, CPU, and Runtime option configuration
- **Sync & async inference** — `Run` and `RunAsync` APIs
- **Tensor buffer management** — Lock/Unlock pattern with managed and host-memory buffers

## Supported Platforms

| Platform | Architecture | Status |
|---|---|---|
| Android | arm64-v8a | Verified (v2.1.2) |
| Windows | x86_64 | Build script ready |
| macOS | Apple Silicon | Planned |
| iOS | arm64 | Planned |

## Requirements

- Unity 6000.3.2f1 or later (Unity 6 LTS)
- .NET 4.7.1

## Installation

### Unity Package Manager (git URL)

1. Open **Window → Package Manager**
2. Click **+** → **Add package from git URL**
3. Enter:

```
https://github.com/ayutaz/uLiteRT.git?path=Assets/LiteRT
```

> **Note:** Native libraries are not included in the repository. See [Building Native Libraries](#building-native-libraries) to build them for your target platform.

## Quick Start

```csharp
using LiteRT;

// 1. Create environment
using var environment = new LiteRtEnvironment();

// 2. Load model
using var model = LiteRtModel.FromFile(modelPath);

// 3. Configure options
using var options = new LiteRtOptions();
options.SetHardwareAccelerators(LiteRtHwAccelerators.kLiteRtHwAcceleratorCpu);

// 4. Compile model
using var compiledModel = new LiteRtCompiledModel(environment, model, options);

// 5. Create I/O buffers from model requirements
using var inputBuffer = LiteRtTensorBuffer.CreateFromRequirements(
    environment, compiledModel, model,
    tensorIndex: 0, isInput: true);
using var outputBuffer = LiteRtTensorBuffer.CreateFromRequirements(
    environment, compiledModel, model,
    tensorIndex: 0, isInput: false);

// 6. Write input data
inputBuffer.WriteFloat(inputData);

// 7. Run inference
compiledModel.Run(
    new[] { inputBuffer },
    new[] { outputBuffer });

// 8. Read output
float[] result = outputBuffer.ReadFloat();
```

## API Overview

| Class | Description |
|---|---|
| `LiteRtEnvironment` | Runtime environment initialization |
| `LiteRtModel` | Model loading and signature info |
| `LiteRtOptions` | Compilation options builder |
| `LiteRtCompiledModel` | Inference execution (sync/async) |
| `LiteRtTensorBuffer` | I/O buffer management (Lock/Unlock) |
| `GpuOptions` | GPU-specific configuration |
| `CpuOptions` | CPU-specific configuration |
| `RuntimeOptions` | Runtime configuration |
| `LiteRtTensorInfo` | Tensor metadata (read-only) |

### API Call Flow

```
Initialize:  Environment → Model → Options → CompiledModel → TensorBuffer
Inference:   WriteFloat(input) → Run → ReadFloat(output)
Dispose:     TensorBuffer → CompiledModel → Options → Model → Environment (reverse order)
```

## Building Native Libraries

Native libraries must be built separately for each target platform.

### Prerequisites

| Platform | Requirement |
|---|---|
| Android | Docker |
| Windows | Bazel |

### Android (arm64)

```bash
bash BuildScripts/build_all.sh
```

Output: `Assets/Plugins/Android/arm64-v8a/libLiteRt.so`

### Windows (x86_64)

```bat
BuildScripts\build_native.bat
```

Output: `Assets/Plugins/Windows/x86_64/libLiteRt.dll`

> LiteRT source is expected at `../LiteRT/` relative to this repository. Currently verified with LiteRT **v2.1.2** tag.

## License

This project is licensed under the [Apache License 2.0](LICENSE).

uLiteRT is a third-party binding library and is not affiliated with or endorsed by Google.
[LiteRT](https://github.com/google-ai-edge/LiteRT) is developed by the Google AI Edge team under the Apache License 2.0.
