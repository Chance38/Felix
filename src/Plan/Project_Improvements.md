# Felix 專案改進計畫

## 目標

參考 jkopay-member 專案架構，實作以下 5 項改進：

1. 新增 `.editorconfig` 程式碼風格配置
2. 實作 `GlobalExceptionHandler` 全域錯誤處理
3. 整合 Serilog 結構化日誌系統
4. 優化 Endpoint 組織結構
5. 整合 Husky.NET Git Hooks

---

## 1. 新增 .editorconfig

**新增檔案：** `/.editorconfig`

**內容包含：**

- 基本格式設定（縮排 4 格、UTF-8 編碼、LF 行尾）
- .NET 命名規則
  - 介面以 `I` 開頭
  - 私有欄位使用 `_camelCase`
  - 常數使用 `PascalCase`
  - 非同步方法以 `Async` 結尾
- C# 風格偏好
  - File-scoped namespace
  - 偏好使用 `var`
  - Expression-bodied members
- 程式碼分析規則
  - IDE0005: 移除不必要的 using
  - IDE0055: 格式化規則
  - CS8618: Non-nullable 欄位警告

---

## 2. GlobalExceptionHandler

**新增檔案：**

- `src/Felix.Api/ExceptionHandlers/GlobalExceptionHandler.cs`

**修改檔案：**

- `src/Felix.Api/Program.cs` - 註冊 ExceptionHandler

**實作要點：**

```csharp
public class GlobalExceptionHandler(ILoggerFactory loggerFactory) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // 1. 設定 Response Content-Type 和 StatusCode
        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = 500;

        // 2. 回傳統一錯誤格式
        var response = new ApiErrorResponse
        {
            Errors = [new ApiError { Message = "Internal Server Error" }]
        };
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        // 3. 動態建立 Logger（記錄真實錯誤來源類別）
        var sourceType = exception.TargetSite?.DeclaringType ?? typeof(GlobalExceptionHandler);
        var logger = loggerFactory.CreateLogger(sourceType);
        logger.LogError(exception, "{ExceptionMessage}", exception.Message);

        return true;
    }
}
```

**Program.cs 修改：**

```csharp
// 註冊
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// 使用
app.UseExceptionHandler();
```

---

## 3. Serilog 整合

**新增套件至 `Felix.Api.csproj`：**

```xml
<PackageReference Include="Serilog.AspNetCore" Version="9.*" />
<PackageReference Include="Serilog.Exceptions" Version="8.*" />
<PackageReference Include="Serilog.Formatting.Compact" Version="3.*" />
```

**Program.cs 配置：**

```csharp
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Compact;

// Bootstrap Logger - 應用啟動階段的日誌
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithExceptionDetails()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // 配置 Serilog
    builder.Host.UseSerilog((ctx, config) =>
    {
        config.ReadFrom.Configuration(ctx.Configuration);
    });

    // ... 其他配置

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
```

**appsettings.json Serilog 配置：**

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Exceptions"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning"
      }
    },
    "Filter": [
      {
        "Name": "ByExcluding",
        "Args": {
          "expression": "RequestPath like '/health%'"
        }
      }
    ],
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "formatter": "Serilog.Formatting.Compact.RenderedCompactJsonFormatter, Serilog.Formatting.Compact"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithExceptionDetails"]
  }
}
```

---

## 4. Endpoint 組織結構優化

**目前結構：**

```
Endpoints/
├── EndpointRoutes.cs              # 所有路由集中定義
└── Assistant/
    ├── Process/
    ├── Model/
    └── EditModel/
```

**新結構：**

```
Endpoints/
├── EndpointExtensions.cs          # 全域註冊入口
└── Assistant/
    ├── AssistantEndpoints.cs      # Assistant 群組註冊
    ├── Process/
    │   └── ProcessEndpoint.cs     # 含 MapProcessEndpoint() 擴充方法
    ├── Model/
    │   └── ModelEndpoint.cs       # 含 MapModelEndpoint() 擴充方法
    └── EditModel/
        └── EditModelEndpoint.cs   # 含 MapEditModelEndpoint() 擴充方法
```

**EndpointExtensions.cs：**

```csharp
namespace Felix.Api.Endpoints;

public static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapAssistantEndpoints();
        return app;
    }
}
```

**AssistantEndpoints.cs：**

```csharp
namespace Felix.Api.Endpoints.Assistant;

public static class AssistantEndpoints
{
    public static IEndpointRouteBuilder MapAssistantEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapProcessEndpoint();
        app.MapModelEndpoint();
        app.MapEditModelEndpoint();
        return app;
    }
}
```

**ProcessEndpoint.cs 修改：**

```csharp
namespace Felix.Api.Endpoints.Assistant.Process;

public static class ProcessEndpoint
{
    public static IEndpointRouteBuilder MapProcessEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/assistant/process", HandleAsync)
            .AddEndpointFilter<ValidationFilter<ProcessRequest>>();
        return app;
    }

    private static async Task<IResult> HandleAsync(/* 參數 */)
    {
        // 實作邏輯
    }
}
```

---

## 5. Husky.NET Git Hooks

**新增檔案結構：**

```
.config/
└── dotnet-tools.json

.husky/
├── _/
│   └── husky.sh
├── pre-commit
├── commit-msg
└── task-runner.json
```

**.config/dotnet-tools.json：**

```json
{
  "version": 1,
  "isRoot": true,
  "tools": {
    "husky": {
      "version": "0.7.2",
      "commands": ["husky"]
    }
  }
}
```

**.husky/task-runner.json：**

```json
{
  "$schema": "https://alirezanet.github.io/Husky.Net/schema.json",
  "tasks": [
    {
      "name": "dotnet-format-staged",
      "command": "bash",
      "group": "pre-commit",
      "args": [
        "-c",
        "FILES=$(git diff --cached --name-only --diff-filter=ACM \"*.cs\" | sed 's| |\\\\ |g'); [ -z \"$FILES\" ] && exit 0; INCLUDE=$(echo \"$FILES\" | xargs | sed -e 's/ /,/g'); if ! dotnet format --verify-no-changes --include \"$INCLUDE\"; then echo 'Run \"dotnet format\" to fix formatting issues.'; exit 1; fi"
      ]
    },
    {
      "name": "commit-msg-linter",
      "command": "bash",
      "group": "commit-msg",
      "args": [
        "-c",
        "MSG=$(head -1 \"${args}\"); PATTERN='^(feat|fix|docs|style|refactor|perf|test|build|ci|chore|revert)(\\(.+\\))?(!)?: .{1,}$'; if ! [[ \"$MSG\" =~ $PATTERN ]]; then echo 'Invalid commit message format. Use: <type>(<scope>): <description>'; exit 1; fi"
      ]
    }
  ]
}
```

**安裝步驟：**

```bash
# 1. 還原工具
dotnet tool restore

# 2. 安裝 Husky hooks
dotnet husky install
```

---

## 實作順序

| 順序 | 項目 | 相依性 |
|------|------|--------|
| 1 | `.editorconfig` | 無 |
| 2 | NuGet 套件 (Serilog) | 無 |
| 3 | `GlobalExceptionHandler` | Serilog |
| 4 | Program.cs 整合 | #2, #3 |
| 5 | Endpoint 結構重構 | 無 |
| 6 | Husky.NET | 無 |

---

## 關鍵檔案清單

| 操作 | 檔案路徑 |
|------|----------|
| 新增 | `.editorconfig` |
| 新增 | `src/Felix.Api/ExceptionHandlers/GlobalExceptionHandler.cs` |
| 新增 | `src/Felix.Api/Endpoints/Assistant/AssistantEndpoints.cs` |
| 新增 | `.config/dotnet-tools.json` |
| 新增 | `.husky/_/husky.sh` |
| 新增 | `.husky/pre-commit` |
| 新增 | `.husky/commit-msg` |
| 新增 | `.husky/task-runner.json` |
| 修改 | `src/Felix.Api/Felix.Api.csproj` |
| 修改 | `src/Felix.Api/Program.cs` |
| 修改 | `src/Felix.Api/appsettings.json` |
| 重構 | `src/Felix.Api/Endpoints/EndpointRoutes.cs` → `EndpointExtensions.cs` |
| 修改 | `src/Felix.Api/Endpoints/Assistant/Process/ProcessEndpoint.cs` |
| 修改 | `src/Felix.Api/Endpoints/Assistant/Model/ModelEndpoint.cs` |
| 修改 | `src/Felix.Api/Endpoints/Assistant/EditModel/EditModelEndpoint.cs` |

---

## 驗證方式

```bash
# 1. 編譯測試
dotnet build

# 2. 格式檢查
dotnet format --verify-no-changes

# 3. 執行應用
dotnet run --project src/Felix.Api

# 4. 驗證 Husky Hook
git commit --allow-empty -m "test: verify husky hook"

# 5. 驗證錯誤處理 - 檢查日誌輸出為 JSON 格式
```
