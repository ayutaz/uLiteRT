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

// LiteRT Unity バインディング — 例外クラステスト

using System;
using NUnit.Framework;

namespace LiteRT.Tests
{
    /// <summary>
    /// LiteRtException のコンストラクタとプロパティのテスト。
    /// ネイティブライブラリなしで実行可能。
    /// </summary>
    public class ExceptionTests
    {
        [Test]
        public void LiteRtException_Constructor_SetsStatusAndMessage()
        {
            var ex = new LiteRtException(LiteRtStatus.ErrorInvalidArgument, "テストメッセージ");
            Assert.AreEqual(LiteRtStatus.ErrorInvalidArgument, ex.Status);
            Assert.AreEqual("テストメッセージ", ex.Message);
        }

        [Test]
        public void LiteRtException_InheritsFromException()
        {
            var ex = new LiteRtException(LiteRtStatus.ErrorRuntimeFailure, "test");
            Assert.IsInstanceOf<Exception>(ex);
        }

        [Test]
        public void LiteRtException_Status_Property_ReturnsCorrectValue()
        {
            var statuses = new[]
            {
                LiteRtStatus.ErrorInvalidArgument,
                LiteRtStatus.ErrorMemoryAllocationFailure,
                LiteRtStatus.ErrorRuntimeFailure,
                LiteRtStatus.ErrorUnsupported,
                LiteRtStatus.ErrorNotFound,
                LiteRtStatus.Cancelled,
            };

            foreach (var status in statuses)
            {
                var ex = new LiteRtException(status, $"Error: {status}");
                Assert.AreEqual(status, ex.Status,
                    $"Status プロパティが {status} と一致しません。");
            }
        }

        [Test]
        public void LiteRtException_CanBeCaughtAsException()
        {
            try
            {
                throw new LiteRtException(LiteRtStatus.ErrorFileIO, "ファイルエラー");
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOf<LiteRtException>(ex);
                Assert.AreEqual(LiteRtStatus.ErrorFileIO, ((LiteRtException)ex).Status);
            }
        }

        [Test]
        public void LiteRtException_MessageContainsStatusInfo()
        {
            var ex = new LiteRtException(LiteRtStatus.ErrorCompilation, "コンパイルに失敗しました");
            Assert.IsTrue(ex.Message.Contains("コンパイル"),
                "メッセージにエラー内容が含まれていません。");
        }
    }
}
