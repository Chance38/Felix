# 對話上下文功能

## 狀態：✅ 已完成

## 技術選型
- **儲存**：Redis（Docker，port 4080）
- **序列化**：JSON
- **歷史限制**：100 則訊息（可設定）
- **TTL**：1 天（可設定）

## 架構

```
Felix.Infrastructure/
└── Persistence/
    └── Redis/
        ├── IRedisContext.cs           # 類似 DbContext
        ├── RedisContext.cs            # 實作
        ├── ConversationStore.cs       # Conversation 操作
        ├── ConversationHistory.cs     # 對話歷史模型
        └── ConversationMessage.cs     # 訊息模型
```

## API

### Request
```json
{
  "message": "今天台北天氣如何？",
  "conversationId": "abc123",
  "location": { ... }
}
```

### Response
```json
{
  "response": "...",
  "conversationId": "abc123"
}
```

## 設定 (appsettings.json)

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:4080"
  },
  "Conversation": {
    "MaxMessages": 100,
    "TtlDays": 1
  }
}
```

## 測試方式

1. 啟動 Redis
```bash
docker compose up -d
```

2. 啟動 API
```bash
cd src/Felix.Api && dotnet run
```

3. 第一次請求（不帶 conversationId）
```bash
curl -X POST http://localhost:3080/api/v1/assistant/process \
  -H "Content-Type: application/json" \
  -d '{"message": "我叫小明"}'
```

4. 後續請求（帶上回傳的 conversationId）
```bash
curl -X POST http://localhost:3080/api/v1/assistant/process \
  -H "Content-Type: application/json" \
  -d '{"message": "我叫什麼名字？", "conversationId": "xxx"}'
```
