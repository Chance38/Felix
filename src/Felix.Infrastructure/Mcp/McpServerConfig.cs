namespace Felix.Infrastructure.Mcp;

/// <summary>
/// 單一 MCP Server 的設定
/// </summary>
public class McpServerConfig
{
    /// <summary>
    /// Server 名稱，用於識別和 log
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 執行指令，例如 "npx" 或 "uvx"
    /// </summary>
    public required string Command { get; set; }

    /// <summary>
    /// 指令參數，例如 ["-y", "@dangahagan/weather-mcp@latest"]
    /// </summary>
    public List<string> Arguments { get; set; } = [];

    /// <summary>
    /// 環境變數（如 API Key）
    /// </summary>
    public Dictionary<string, string> Environment { get; set; } = [];

    /// <summary>
    /// 是否啟用此 Server
    /// </summary>
    public bool Enabled { get; set; } = true;
}
