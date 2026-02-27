# 天氣查詢技能

當使用者詢問天氣相關問題時，使用此技能。

## 可用工具

| 工具 | 說明 |
|------|------|
| `get_coordinates(city)` | 將城市名轉為經緯度（需要英文城市名） |
| `get_weather(latitude, longitude)` | 用經緯度查天氣 |
| `get_current_location_weather()` | 用使用者當前位置查天氣 |

## 查詢流程

### 1. 使用者沒指定城市

直接使用 `get_current_location_weather()`。

**範例**：
- 使用者：「現在天氣如何？」
- 行動：呼叫 `get_current_location_weather()`

### 2. 使用者指定城市

先轉座標，再查天氣：

1. 將中文城市名轉為英文（見下方對照表）
2. 呼叫 `get_coordinates(city)` 取得座標
3. 從回傳結果解析 latitude 和 longitude
4. 呼叫 `get_weather(latitude, longitude)` 取得天氣

**範例**：
- 使用者：「中壢天氣如何？」
- 行動：
  1. `get_coordinates("Zhongli")` → 回傳 `latitude=24.96, longitude=121.24`
  2. `get_weather(24.96, 121.24)` → 回傳天氣

## 中文城市名對照表

| 中文 | 英文 | | 中文 | 英文 |
|------|------|---|------|------|
| 台北 | Taipei | | 高雄 | Kaohsiung |
| 新北 | New Taipei | | 屏東 | Pingtung |
| 桃園 | Taoyuan | | 宜蘭 | Yilan |
| 中壢 | Zhongli | | 花蓮 | Hualien |
| 新竹 | Hsinchu | | 台東 | Taitung |
| 苗栗 | Miaoli | | 基隆 | Keelung |
| 台中 | Taichung | | 澎湖 | Penghu |
| 彰化 | Changhua | | 金門 | Kinmen |
| 南投 | Nantou | | 馬祖 | Matsu |
| 雲林 | Yunlin | | 板橋 | Banqiao |
| 嘉義 | Chiayi | | 內湖 | Neihu |
| 台南 | Tainan | | 信義 | Xinyi |

### 國際城市

| 中文 | 英文 | | 中文 | 英文 |
|------|------|---|------|------|
| 東京 | Tokyo | | 倫敦 | London |
| 大阪 | Osaka | | 巴黎 | Paris |
| 首爾 | Seoul | | 紐約 | New York |

## 回應風格

取得天氣資訊後，用自然的方式回答，並適時給予建議：

- 下雨 → 建議帶傘
- 高溫（> 30°C）→ 建議防曬、多喝水
- 低溫（< 15°C）→ 建議添加衣物
- 陰天 → 提醒可能轉雨
