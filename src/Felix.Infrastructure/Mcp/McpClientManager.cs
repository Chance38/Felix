using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

namespace Felix.Infrastructure.Mcp;

/// <summary>
/// 管理所有 MCP Server 連線
/// </summary>
public interface IMcpClientManager : IAsyncDisposable
{
    /// <summary>
    /// 初始化並連接所有已啟用的 MCP Server
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得所有已連接的 MCP Client
    /// </summary>
    IReadOnlyList<IMcpClient> GetClients();
}

public class McpClientManager : IMcpClientManager
{
    private readonly List<McpServerConfig> _servers;
    private readonly ILogger<McpClientManager> _logger;
    private readonly List<IMcpClient> _clients = [];
    private bool _initialized;

    public McpClientManager(
        IConfiguration configuration,
        ILogger<McpClientManager> logger)
    {
        _servers = configuration.GetSection("Mcp:Servers").Get<List<McpServerConfig>>() ?? [];
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized) return;

        var enabledServers = _servers.Where(s => s.Enabled).ToList();

        if (enabledServers.Count == 0)
        {
            _logger.LogWarning("沒有啟用的 MCP Server");
            _initialized = true;
            return;
        }

        _logger.LogInformation("正在連接 {Count} 個 MCP Server...", enabledServers.Count);

        foreach (var serverConfig in enabledServers)
        {
            try
            {
                var client = await ConnectToServerAsync(serverConfig, cancellationToken);
                _clients.Add(client);

                // 列出此 Server 提供的工具
                var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);
                _logger.LogInformation(
                    "已連接 MCP Server: {Name}，提供 {ToolCount} 個工具",
                    serverConfig.Name,
                    tools.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "無法連接 MCP Server: {Name}", serverConfig.Name);
                throw new InvalidOperationException($"無法連接 MCP Server: {serverConfig.Name}", ex);
            }
        }

        _initialized = true;
    }

    public IReadOnlyList<IMcpClient> GetClients()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("McpClientManager 尚未初始化，請先呼叫 InitializeAsync");
        }

        return _clients.AsReadOnly();
    }

    private async Task<IMcpClient> ConnectToServerAsync(
        McpServerConfig config,
        CancellationToken cancellationToken)
    {
        // 建立 stdio 傳輸層
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = config.Name,
            Command = config.Command,
            Arguments = [.. config.Arguments],
            EnvironmentVariables = config.Environment.Count > 0
                ? config.Environment.ToDictionary(kvp => kvp.Key, kvp => (string?)kvp.Value)
                : null
        });

        // 建立並連接 MCP Client
        var client = await McpClientFactory.CreateAsync(
            transport,
            cancellationToken: cancellationToken);

        return client;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var client in _clients)
        {
            try
            {
                await client.DisposeAsync();
            }
            catch (Exception)
            {
                // MCP Server 可能已經被關閉，忽略錯誤
            }
        }

        _clients.Clear();
        _initialized = false;

        GC.SuppressFinalize(this);
    }
}
