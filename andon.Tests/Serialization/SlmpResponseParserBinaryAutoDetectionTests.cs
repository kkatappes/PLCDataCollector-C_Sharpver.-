using System;
using System.Text;
using SlmpClient.Constants;
using SlmpClient.Exceptions;
using SlmpClient.Serialization;
using Xunit;
using Xunit.Abstractions;

namespace SlmpClient.Tests.Serialization
{
    /// <summary>
    /// TDDテスト: 0xD0バイトエラー対応
    /// Phase 4実装後のPLC応答変化に対応したバイナリ/ASCII自動判定機能
    /// Red-Green-Refactor サイクルに従って実装
    /// </summary>
    public class SlmpResponseParserBinaryAutoDetectionTests
    {
        private readonly ITestOutputHelper _output;

        public SlmpResponseParserBinaryAutoDetectionTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// 🔴 RED: 0xD0バイトエラーを再現する失敗するテスト
        /// Phase 4実装後、PLCがバイナリ応答を返すのに、ASCIIモードで解析を試行してエラー発生
        /// </summary>
        [Fact]
        public void ParseResponse_Should_Fail_With_0xD0_Byte_Error_Before_Fix()
        {
            // Arrange: ASCII 16進形式で無効文字を含むレスポンスフレーム
            // "D0"は有効だが、0xD0バイト（208）は無効16進文字としてエラーになる
            var invalidAsciiResponse = "D0000000000000000000" + (char)0xD0 + "0000"; // 22文字、0xD0バイトを含む
            var responseBytes = System.Text.Encoding.ASCII.GetBytes(invalidAsciiResponse.Replace(((char)0xD0).ToString(), "G0")); // Gは無効16進文字

            _output.WriteLine($"Testing ASCII response with invalid hex char: {System.Text.Encoding.ASCII.GetString(responseBytes)}");
            _output.WriteLine("Expected: ArgumentException with '無効な16進文字' message");

            // Act & Assert: ASCIIモード（isBinary: false）で解析すると無効16進文字エラーが発生
            var exception = Assert.Throws<ArgumentException>(() =>
            {
                SlmpResponseParser.ParseResponse(responseBytes, isBinary: false, SlmpFrameVersion.Version4E);
            });

            // 無効16進文字エラーの具体的なメッセージを確認
            Assert.Contains("無効な16進文字", exception.Message);

            _output.WriteLine($"✅ RED: 無効16進文字エラーを再現 - {exception.Message}");
        }

        /// <summary>
        /// 🔴 RED: バイナリ/ASCII自動判定機能のテスト（未実装段階）
        /// 実装前なので IsBinaryResponse メソッドが存在しないためコンパイルエラー
        /// </summary>
        [Fact]
        public void IsBinaryResponse_Should_Detect_Binary_Format_With_Suspicious_Bytes()
        {
            // Arrange: 0xD0, 0xDE, 0xAD, 0xBE, 0xEF などの疑わしいバイトを含むデータ
            var binaryDataWithSuspiciousBytes = new byte[]
            {
                0xD0, 0x00, 0x00, 0x00, // 0xD0を含む
                0xDE, 0xAD, 0xBE, 0xEF  // DEADBEEFパターン
            };

            var asciiData = Encoding.ASCII.GetBytes("50000000000000000000");

            _output.WriteLine($"Testing binary detection for: {Convert.ToHexString(binaryDataWithSuspiciousBytes)}");

            // Act & Assert: 自動判定機能テスト（実装後に通る予定）
            // 現在は IsBinaryResponse メソッドが存在しないため失敗
            try
            {
                // この行でコンパイルエラーまたはランタイムエラー（メソッド未実装）
                bool isBinary = SlmpResponseParser.IsBinaryResponse(binaryDataWithSuspiciousBytes);
                bool isAscii = SlmpResponseParser.IsBinaryResponse(asciiData);

                Assert.True(isBinary, "疑わしいバイトを含むデータはバイナリと判定されるべき");
                Assert.False(isAscii, "ASCII文字のみのデータはASCIIと判定されるべき");

                _output.WriteLine("✅ バイナリ/ASCII自動判定成功");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"🔴 RED: バイナリ/ASCII自動判定機能未実装 - {ex.Message}");
                throw; // RED段階では失敗が期待される
            }
        }

        /// <summary>
        /// 🔴 RED: フォールバック処理のテスト（未実装段階）
        /// ParseResponseで形式判定エラー時の自動再試行機能
        /// </summary>
        [Fact]
        public void ParseResponse_Should_Fallback_When_Format_Detection_Fails()
        {
            // Arrange: バイナリデータをASCIIモードで解析してエラー発生（20バイト以上）
            var binaryResponse = new byte[]
            {
                0xD0, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00  // 20バイト
            };

            _output.WriteLine("Testing fallback mechanism for format detection error");

            // Act & Assert: フォールバック処理テスト（実装後に通る予定）
            try
            {
                // 現在の実装ではフォールバック処理がないため、0xD0バイトエラーで失敗
                var response = SlmpResponseParser.ParseResponse(binaryResponse, isBinary: false, SlmpFrameVersion.Version4E);

                // 実装後の期待される動作：
                // 1. ASCIIモードで解析 → 0xD0バイトエラー
                // 2. 自動判定でバイナリと認識
                // 3. バイナリモードで再試行 → 成功
                Assert.NotNull(response);
                Assert.Equal(EndCode.Success, response.EndCode);

                _output.WriteLine("✅ フォールバック処理成功");
            }
            catch (ArgumentException ex) when (ex.Message.Contains("無効な16進文字"))
            {
                _output.WriteLine($"🔴 RED: フォールバック処理未実装 - {ex.Message}");
                throw; // RED段階では失敗が期待される
            }
        }

        /// <summary>
        /// 🔴 RED: 境界値テスト - Phase 4で特定された問題パターン
        /// </summary>
        [Theory]
        [InlineData(new byte[] { 0xD0 }, "0xD0 single byte")]
        [InlineData(new byte[] { 0xDE, 0xAD }, "0xDEAD pattern")]
        [InlineData(new byte[] { 0xBE, 0xEF }, "0xBEEF pattern")]
        [InlineData(new byte[] { 0x00, 0x1F }, "Below ASCII printable range")]
        [InlineData(new byte[] { 0x7F, 0x80 }, "Above ASCII printable range")]
        public void IsBinaryResponse_Should_Handle_Boundary_Cases(byte[] testData, string description)
        {
            _output.WriteLine($"Testing boundary case: {description} - {Convert.ToHexString(testData)}");

            // Act & Assert: 境界値テスト（実装後に通る予定）
            try
            {
                bool result = SlmpResponseParser.IsBinaryResponse(testData);
                Assert.True(result, $"{description} should be detected as binary");

                _output.WriteLine("✅ 境界値テスト成功");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"🔴 RED: 境界値テスト失敗（実装前） - {ex.Message}");
                throw; // RED段階では失敗が期待される
            }
        }

        /// <summary>
        /// 🔴 RED: SOLID原則 - 単一責任原則テスト
        /// 自動判定機能が独立した責任を持つかのテスト
        /// </summary>
        [Fact]
        public void IsBinaryResponse_Should_Follow_Single_Responsibility_Principle()
        {
            // Arrange: 判定専用の機能として独立していることを確認
            var validBinaryData = new byte[] { 0xD0, 0x00, 0x00, 0x00 };
            var validAsciiData = Encoding.ASCII.GetBytes("5000");

            // Act & Assert: 判定機能のみに集中し、副作用がないことを確認
            try
            {
                // 判定結果は一貫性があり、副作用がない
                bool result1 = SlmpResponseParser.IsBinaryResponse(validBinaryData);
                bool result2 = SlmpResponseParser.IsBinaryResponse(validBinaryData);
                bool result3 = SlmpResponseParser.IsBinaryResponse(validAsciiData);

                Assert.Equal(result1, result2); // 一貫性
                Assert.NotEqual(result1, result3); // 正確性

                _output.WriteLine("✅ 単一責任原則テスト成功");
            }
            catch (Exception ex)
            {
                _output.WriteLine($"🔴 RED: 単一責任原則テスト失敗（実装前） - {ex.Message}");
                throw; // RED段階では失敗が期待される
            }
        }
    }
}