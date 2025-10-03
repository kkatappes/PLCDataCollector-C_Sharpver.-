using System;
using System.Text;
using SlmpClient.Constants;
using SlmpClient.Exceptions;
using SlmpClient.Serialization;
using Xunit;
using Xunit.Abstractions;
using TypeCode = SlmpClient.Constants.TypeCode;

namespace SlmpClient.Tests
{
    /// <summary>
    /// SLMP ASCIIフレーム解析エラー対応のTDDテスト
    /// 手順書に基づく20文字ASCII応答の処理テスト
    /// </summary>
    public class SlmpResponseParser20CharAsciiTests
    {
        private readonly ITestOutputHelper _output;

        public SlmpResponseParser20CharAsciiTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// Green段階: 20文字ASCII応答が修正後に成功することを確認
        /// 手順書で確認された実際のPLC応答パターンをテスト
        /// </summary>
        [Fact]
        public void ParseAsciiResponse_Should_Success_With_20Chars_After_Fix()
        {
            // Arrange: 20文字のASCII応答フレーム（4E形式の最小応答）
            // 実際のPLCからの応答パターンを模擬
            var ascii20CharResponse = "50000000000000000000";
            var responseBytes = Encoding.ASCII.GetBytes(ascii20CharResponse);

            _output.WriteLine($"Testing 20-char ASCII response: {ascii20CharResponse}");
            _output.WriteLine($"Response length: {responseBytes.Length} chars");

            // Act: 修正後の実装では20文字でも成功する
            var response = SlmpResponseParser.ParseResponse(responseBytes, isBinary: false, SlmpFrameVersion.Version4E);

            // Assert: 正常に解析される
            Assert.NotNull(response);
            Assert.Equal(EndCode.Success, response.EndCode);
            _output.WriteLine($"SUCCESS: 20-char ASCII response accepted after fix");
        }

        /// <summary>
        /// Red段階: 正常な22文字ASCII応答は成功することを確認（回帰テスト）
        /// </summary>
        [Fact]
        public void ParseAsciiResponse_Should_Success_With_22Chars_NormalCase()
        {
            // Arrange: 22文字の正常なASCII応答フレーム
            // 4Eフレーム: サブヘッダー(4) + シーケンス(4) + 予約(4) + Network(2) + Node(2) + DestProc(4) + MultiDrop(2) = 22文字最小
            var ascii22CharResponse = "5000000000000000000000";
            var responseBytes = Encoding.ASCII.GetBytes(ascii22CharResponse);

            _output.WriteLine($"Testing 22-char ASCII response: {ascii22CharResponse}");

            // Act: 現在の実装で正常に処理されることを確認
            var response = SlmpResponseParser.ParseResponse(responseBytes, isBinary: false, SlmpFrameVersion.Version4E);

            // Assert: 正常に解析される
            Assert.NotNull(response);
            Assert.Equal(EndCode.Success, response.EndCode);
        }

        /// <summary>
        /// Red段階: 20文字応答が受け入れられるべきテスト（将来の実装で成功予定）
        /// 手順書の分析結果に基づく期待される動作
        /// </summary>
        [Fact]
        public void ParseAsciiResponse_Should_Accept_20Chars_After_Fix()
        {
            // Arrange: 20文字の有効なASCII応答フレーム（4E形式、最小構成）
            // 4E形式: SubHeader(4) + Sequence(4) + Reserved(4) + Network(2) + Node(2) + DestProc(4) = 20文字
            // EndCode(0000)を含む最小有効応答
            var ascii20CharResponse = "D0000000000000000000"; // サブヘッダーD000、他は0パディング
            var responseBytes = Encoding.ASCII.GetBytes(ascii20CharResponse);

            _output.WriteLine($"Testing 20-char ASCII response acceptance: {ascii20CharResponse}");

            // Act & Assert: 修正後は20文字でも正常に処理されるべき
            // 現在は失敗するが、修正後は成功するはず
            try
            {
                var response = SlmpResponseParser.ParseResponse(responseBytes, isBinary: false, SlmpFrameVersion.Version4E);

                // 修正後の期待される動作
                Assert.NotNull(response);
                Assert.Equal(EndCode.Success, response.EndCode);
                _output.WriteLine("SUCCESS: 20-char ASCII response accepted after fix");
            }
            catch (SlmpCommunicationException ex) when (ex.Message.Contains("Response frame too short"))
            {
                // 現在の実装では失敗する（修正前）
                _output.WriteLine($"EXPECTED FAILURE (before fix): {ex.Message}");
                throw; // Red段階では失敗が期待される
            }
        }

        /// <summary>
        /// テストデータ生成ヘルパー: 手順書の分析結果に基づく実際のPLC応答パターン
        /// </summary>
        [Theory]
        [InlineData("50000000000000000000", 20, "4E ASCII minimum response")]
        [InlineData("5000000000000000000000", 22, "4E ASCII standard response")]
        [InlineData("500000000000000000000000", 24, "4E ASCII with data response")]
        public void ParseAsciiResponse_VariousLengths_ShouldHandleAppropriately(string asciiResponse, int expectedLength, string description)
        {
            // Arrange
            var responseBytes = Encoding.ASCII.GetBytes(asciiResponse);
            _output.WriteLine($"Testing {description}: {asciiResponse} ({expectedLength} chars)");

            // Act & Assert: 修正後は20文字以上で成功
            if (expectedLength >= 20)
            {
                // 20文字以上は修正後の実装で成功
                var response = SlmpResponseParser.ParseResponse(responseBytes, isBinary: false, SlmpFrameVersion.Version4E);
                Assert.NotNull(response);
                Assert.Equal(EndCode.Success, response.EndCode);
                _output.WriteLine($"Success for {expectedLength} chars");
            }
            else
            {
                // 20文字未満は失敗
                var exception = Assert.Throws<SlmpCommunicationException>(() =>
                {
                    SlmpResponseParser.ParseResponse(responseBytes, isBinary: false, SlmpFrameVersion.Version4E);
                });
                Assert.Contains("Response frame too short", exception.Message);
                _output.WriteLine($"Expected failure for {expectedLength} chars: {exception.Message}");
            }
        }

        /// <summary>
        /// 境界値テスト: 手順書で特定された問題の境界を確認
        /// </summary>
        [Theory]
        [InlineData(18, "Too short even for 3E")]
        [InlineData(19, "Just under 20")]
        [InlineData(20, "Actual PLC response length")]
        [InlineData(21, "One char over")]
        [InlineData(22, "Current expected minimum")]
        public void ParseAsciiResponse_BoundaryValues_4E_ASCII(int responseLength, string description)
        {
            // Arrange: 指定された長さのASCII応答を生成
            var asciiResponse = new string('0', responseLength);
            var responseBytes = Encoding.ASCII.GetBytes(asciiResponse);

            _output.WriteLine($"Testing boundary: {description} ({responseLength} chars)");

            // Act & Assert: 修正後は20文字以上で成功
            if (responseLength < 20)
            {
                // 20文字未満は失敗
                var exception = Assert.Throws<SlmpCommunicationException>(() =>
                {
                    SlmpResponseParser.ParseResponse(responseBytes, isBinary: false, SlmpFrameVersion.Version4E);
                });
                Assert.Contains("Response frame too short", exception.Message);
                _output.WriteLine($"Failed as expected: {exception.Message}");
            }
            else if (responseLength == 21)
            {
                // 21文字（奇数）は別のエラー
                var exception = Assert.Throws<SlmpCommunicationException>(() =>
                {
                    SlmpResponseParser.ParseResponse(responseBytes, isBinary: false, SlmpFrameVersion.Version4E);
                });
                Assert.Contains("Invalid ASCII response data length", exception.Message);
                _output.WriteLine($"Failed with data length error: {exception.Message}");
            }
            else
            {
                // 20, 22文字以上では成功
                var response = SlmpResponseParser.ParseResponse(responseBytes, isBinary: false, SlmpFrameVersion.Version4E);
                Assert.NotNull(response);
                Assert.Equal(EndCode.Success, response.EndCode);
                _output.WriteLine("Succeeded as expected");
            }
        }
    }
}