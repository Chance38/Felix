using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace Felix.Infrastructure.McpClients;

/// <summary>
/// MCP Client 管理器實作
/// </summary>
public sealed class McpClientManager(
    IOptions<McpOptions> options,
    ILogger<McpClientManager> logger) : IMcpClientManager
{
    private readonly List<IMcpClient> _clients = [];
    private readonly Dictionary<IMcpClient, IList<McpClientTool>> _toolCache = [];
    private bool _initialized;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_initialized) return;

        var servers = options.Value.Servers;
        var enabledServers = servers.Where(s => s.Enabled).ToList();

        if (enabledServers.Count == 0)
        {
            logger.LogWarning("沒有啟用的 MCP Server");
            _initialized = true;
            return;
        }

        logger.LogInformation("正在連接 {Count} 個 MCP Server...", enabledServers.Count);

        foreach (var serverConfig in enabledServers)
        {
            try
            {
                var client = await ConnectToServerAsync(serverConfig, ct);
                _clients.Add(client);

                // 列出並快取此 Server 提供的工具
                var tools = await client.ListToolsAsync(cancellationToken: ct);
                _toolCache[client] = tools;
                logger.LogInformation(
                    "已連接 MCP Server: {Name}，提供 {ToolCount} 個工具",
                    serverConfig.Name,
                    tools.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "無法連接 MCP Server: {Name}", serverConfig.Name);
                throw new InvalidOperationException($"無法連接 MCP Server: {serverConfig.Name}", ex);
            }
        }

        _initialized = true;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<IMcpClient, IList<McpClientTool>> GetCachedTools()
    {
        if (!_initialized)
            throw new InvalidOperationException("McpClientManager 尚未初始化，請先呼叫 InitializeAsync");

        return _toolCache;
    }

    private static async Task<IMcpClient> ConnectToServerAsync(
        McpServerConfig config,
        CancellationToken ct)
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
            cancellationToken: ct);

        return client;
    }

    /// <inheritdoc />
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
