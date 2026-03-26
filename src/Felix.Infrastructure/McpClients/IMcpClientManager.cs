using ModelContextProtocol.Client;

namespace Felix.Infrastructure.McpClients;

/// <summary>
/// MCP Client 管理器介面
/// </summary>
public interface IMcpClientManager : IAsyncDisposable
{
    /// <summary>
    /// 初始化並連接所有已啟用的 MCP Server
    /// </summary>
    Task InitializeAsync(CancellationToken ct = default);

    /// <summary>
    /// 取得初始化時快取的工具清單（key: client, value: 該 client 的工具）
    /// </summary>
    IReadOnlyDictionary<IMcpClient, IList<McpClientTool>> GetCachedTools();
}
