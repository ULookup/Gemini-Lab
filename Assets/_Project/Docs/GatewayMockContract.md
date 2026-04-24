# OpenClaw Gateway Mock Contract

## Request Envelope

```json
{
  "traceId": "string",
  "requestType": "Chat|Work|Ack",
  "message": "string",
  "playerId": "string",
  "petState": "Idle|Moving|Interacting|Working|Sleeping",
  "personality": "string",
  "contentJson": "{}",
  "isAck": false
}
```

## Response Envelope

```json
{
  "traceId": "string",
  "success": true,
  "message": "string",
  "payloadJson": "{}",
  "httpStatusCode": 200
}
```

## Event Payload Examples

### Chat chunk

```json
{
  "traceId": "trace_chat_001",
  "eventType": 1,
  "message": "正在思考你的请求...",
  "payloadJson": "{}"
}
```

### Work done

```json
{
  "traceId": "trace_work_001",
  "eventType": 4,
  "message": "任务完成，已同步结果。",
  "payloadJson": "{}"
}
```

### Error

```json
{
  "traceId": "trace_work_001",
  "eventType": 6,
  "message": "gateway timeout",
  "errorCode": "TIMEOUT",
  "payloadJson": "{}"
}
```
