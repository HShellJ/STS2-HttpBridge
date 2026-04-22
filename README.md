# STS2-HttpBridge

杀戮尖塔2 HTTP API 桥接 Mod。通过 REST API 暴露游戏状态，支持外部命令控制（出牌、结束回合、选择奖励等 30+ 种操作），适用于 AI 自动游玩。

## 依赖

- [BaseLib](https://github.com/Alchyr/BaseLib-StS2)

## API

| 端点 | 方法 | 说明 |
|------|------|------|
| `/api/health` | GET | 健康检查 |
| `/api/state` | GET | 获取游戏状态 |
| `/api/command` | POST | 发送游戏操作命令 |
| `/api/config` | GET/POST | 配置管理 |

### GET /api/state

返回当前游戏状态快照，包含 combat / event / shop / restSite / treasure / run / map 等数据段。

### POST /api/command

```json
{
  "type": "EndTurn",
  "command": {}
}
```

常用命令类型：`EndTurn`、`PlayCard`、`SelectReward`、`SkipReward`、`SelectEventOption`、`SelectRestOption`、`SelectMapNode`、`Proceed`。

## 配置

首次运行自动生成，路径：`%APPDATA%/SlayTheSpire2/httpbridge/config.json`

```json
{
  "host": "localhost",
  "port": 8080,
  "apiKey": "",
  "enableCors": true,
  "allowedOrigins": "*",
  "stateCacheDurationMs": 100
}
```

## 构建

```bash
dotnet build -p:Sts2Path="path/to/Slay the Spire 2"
```
