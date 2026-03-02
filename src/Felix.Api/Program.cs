using Felix.Api.Endpoints;
using Felix.Infrastructure;
using Felix.Infrastructure.Mcp;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddInfrastructure();

var app = builder.Build();

// 啟動時初始化所有 MCP Server 連線
var mcpManager = app.Services.GetRequiredService<IMcpClientManager>();
await mcpManager.InitializeAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.MapGet("/health", () => Results.Ok("Healthy"));
app.MapEndpoints();

app.Run();
