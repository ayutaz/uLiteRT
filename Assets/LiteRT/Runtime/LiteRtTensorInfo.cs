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

// LiteRT Unity バインディング — テンソル情報ラッパー

using System;
using System.Runtime.InteropServices;

namespace LiteRT
{
    /// <summary>
    /// テンソルの型・名前・量子化情報への読み取り専用アクセスを提供する。
    /// テンソルハンドルは Model/Signature が所有するため IDisposable 不要。
    /// このクラスはスレッドセーフではない。
    /// </summary>
    public sealed class LiteRtTensorInfo
    {
        private readonly IntPtr _tensorHandle;

        internal LiteRtTensorInfo(IntPtr tensorHandle)
        {
            if (tensorHandle == IntPtr.Zero)
                throw new ArgumentException("テンソルハンドルが無効です。", nameof(tensorHandle));
            _tensorHandle = tensorHandle;
        }

        /// <summary>テンソル型の種別を取得する。</summary>
        public LiteRtTensorTypeId TypeId
        {
            get
            {
                LiteRtException.CheckStatus(
                    Native.LiteRtGetTensorTypeId(_tensorHandle, out var typeId));
                return typeId;
            }
        }

        /// <summary>テンソルの名前を取得する。</summary>
        public string Name
        {
            get
            {
                LiteRtException.CheckStatus(
                    Native.LiteRtGetTensorName(_tensorHandle, out var namePtr));
                return Marshal.PtrToStringAnsi(namePtr);
            }
        }

        /// <summary>量子化の種別を取得する。</summary>
        public LiteRtQuantizationTypeId QuantizationTypeId
        {
            get
            {
                LiteRtException.CheckStatus(
                    Native.LiteRtGetQuantizationTypeId(_tensorHandle, out var quantTypeId));
                return quantTypeId;
            }
        }

        /// <summary>ランク付きテンソル型情報を取得する。</summary>
        public LiteRtRankedTensorType GetRankedTensorType()
        {
            LiteRtException.CheckStatus(
                Native.LiteRtGetRankedTensorType(_tensorHandle, out var tensorType));
            return tensorType;
        }

        /// <summary>テンソル単位の量子化パラメータを取得する。</summary>
        public LiteRtQuantizationPerTensor GetQuantizationPerTensor()
        {
            LiteRtException.CheckStatus(
                Native.LiteRtGetQuantizationPerTensor(_tensorHandle, out var perTensor));
            return perTensor;
        }

        /// <summary>チャネル単位の量子化パラメータを取得する。</summary>
        public LiteRtQuantizationPerChannel GetQuantizationPerChannel()
        {
            LiteRtException.CheckStatus(
                Native.LiteRtGetQuantizationPerChannel(_tensorHandle, out var perChannel));
            return perChannel;
        }
    }
}
