using Xunit;
using Andon.Core.Models;
using Andon.Core.Constants;

namespace Andon.Tests.Unit.Core.Models;

/// <summary>
/// ConnectionResponseクラスのテスト（Phase 1: 通信プロトコル自動切り替え機能）
/// </summary>
public class ConnectionResponseTests
{
    #region Phase 1-Red: 新規プロパティのテスト（失敗するテスト）

    [Fact]
    public void UsedProtocol_初期TCP成功時_TCPを返す()
    {
        // Arrange & Act
        var response = new ConnectionResponse
        {
            Status = ConnectionStatus.Connected,
            UsedProtocol = "TCP",  // ← まだ存在しないプロパティ
            IsFallbackConnection = false
        };

        // Assert
        Assert.Equal("TCP", response.UsedProtocol);
    }

    [Fact]
    public void IsFallbackConnection_代替プロトコル使用時_Trueを返す()
    {
        // Arrange & Act
        var response = new ConnectionResponse
        {
            Status = ConnectionStatus.Connected,
            UsedProtocol = "UDP",
            IsFallbackConnection = true,  // ← まだ存在しないプロパティ
            FallbackErrorDetails = "TCP接続失敗"
        };

        // Assert
        Assert.True(response.IsFallbackConnection);
    }

    [Fact]
    public void FallbackErrorDetails_初期プロトコル失敗時_エラー詳細を保持()
    {
        // Arrange & Act
        var response = new ConnectionResponse
        {
            Status = ConnectionStatus.Connected,
            UsedProtocol = "UDP",
            IsFallbackConnection = true,
            FallbackErrorDetails = "TCP接続タイムアウト"  // ← まだ存在しないプロパティ
        };

        // Assert
        Assert.Equal("TCP接続タイムアウト", response.FallbackErrorDetails);
    }

    #endregion

    #region 既存プロパティの基本テスト

    [Fact]
    public void Constructor_必須プロパティのみ_正常に作成される()
    {
        // Arrange & Act
        var response = new ConnectionResponse
        {
            Status = ConnectionStatus.Connected
        };

        // Assert
        Assert.Equal(ConnectionStatus.Connected, response.Status);
        Assert.Null(response.Socket);
        Assert.Null(response.ConnectedAt);
        Assert.Null(response.ConnectionTime);
        Assert.Null(response.ErrorMessage);
    }

    [Fact]
    public void Status_接続成功時_Connectedを返す()
    {
        // Arrange & Act
        var response = new ConnectionResponse
        {
            Status = ConnectionStatus.Connected
        };

        // Assert
        Assert.Equal(ConnectionStatus.Connected, response.Status);
    }

    [Fact]
    public void Status_接続失敗時_Failedを返す()
    {
        // Arrange & Act
        var response = new ConnectionResponse
        {
            Status = ConnectionStatus.Failed,
            ErrorMessage = "接続エラー"
        };

        // Assert
        Assert.Equal(ConnectionStatus.Failed, response.Status);
        Assert.Equal("接続エラー", response.ErrorMessage);
    }

    #endregion
}
