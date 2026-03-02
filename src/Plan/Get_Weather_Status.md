# Felix MCP 整合計畫

## 目標
讓 Felix 作為 MCP Client，連接外部 MCP Server 取得工具能力。
針對台灣地區，使用中央氣象署 API 取得更準確的天氣資料。

---

## 架構設計

```
User: "台北天氣如何"
           │
           ▼
┌───────────────────────────────────┐
│  AI: Taiwan or International?     │
└───────────────────────────────────┘
           │                │
           ▼                ▼
┌─────────────────┐  ┌─────────────────┐
│  Taiwan         │  │  International  │
│  TaiwanWeather  │  │  weather-mcp    │
│  Tool (Local)   │  │  (Open-Meteo)   │
└─────────────────┘  └─────────────────┘
```

---

## MCP Server

### 地理編碼：@geocoding-ai/mcp
- API：OpenStreetMap Nominatim
- 不需 API Key
- 安裝：`npx -y @geocoding-ai/mcp`

### 國際天氣：weather-mcp
- API：NOAA + Open-Meteo
- 不需 API Key
- 安裝：`npx -y @dangahagan/weather-mcp@latest`
- 用途：查詢台灣以外地區的天氣

---

## 已實作項目

### 1. TaiwanWeatherClient ✅
```
src/Felix.Infrastructure/
├── Clients/
│   └── Weather/
│       └── TaiwanWeatherClient.cs  # 介面 + 實作
```
- 使用中央氣象署 36 小時天氣預報 API
- 回傳白天/夜晚溫度、天氣、降雨機率
- 支援雨停預測

### 2. 本地工具系統 ✅
```
src/Felix.Infrastructure/
├── AI/
│   └── Tools/
│       ├── ILocalTool.cs           # 介面
│       └── TaiwanWeatherTool.cs    # 台灣天氣工具
```
- `ILocalTool` 介面統一管理本地工具
- 透過 DI 注入 `IEnumerable<ILocalTool>`

### 3. Felix 核心 ✅
- 工具清單動態產生（本地 + MCP）
- MCP 工具映射快取（避免重複呼叫 ListToolsAsync）
- API Key 輪換機制（429 自動切換）
- GeneratedRegex 優化

### 4. weather.md Skill ✅
- 台灣地區：`get_taiwan_weather`
- 國際地區：`geocode` → `get_forecast`

### 5. 設定 ✅
- `appsettings.json` 新增 `CwaApiKey`
- `DependencyInjection.cs` 註冊 HttpClient 和本地工具

---

## 中央氣象署 API 資訊

### 端點
- 一般天氣預報：`https://opendata.cwa.gov.tw/api/v1/rest/datastore/F-C0032-001`

### 參數
- `Authorization`: API Key
- `locationName`: 縣市名稱（如「臺北市」）

### 回傳時段
- 動態更新（接近中午時，早上時段會被移除）
- 白天 (06:00-18:00)
- 夜晚 (18:00-06:00)

---

## 待辦事項

- [ ] 對話記憶（跨請求的上下文）
- [ ] 更多本地工具

---

## 參考資源

- [中央氣象署開放資料平台](https://opendata.cwa.gov.tw/)
- [ModelContextProtocol NuGet](https://www.nuget.org/packages/ModelContextProtocol)
- [weather-mcp](https://github.com/weather-mcp/weather-mcp)
- [@geocoding-ai/mcp](https://www.npmjs.com/package/@geocoding-ai/mcp)
