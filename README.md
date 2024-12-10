# CTunnel

#### 介绍

.NET内网穿透

#### 使用

##### 服务端

`./CTunnel.Server-linux-x64 AppConfig.json`

```json
// AppConfig.json
{
  "AppConfig": {
    // 服务端连接端口
    "ServerPort": 8888,
    // HTTP端口
    "HttpPort": 80,
    // HTTPS端口
    "HttpsPort": 443,
    // 证书
    "Certificate": "certificate.pem",
    "CertificateKey": "private_key.pem",
    // 认证令牌
    "Token": "TOKEN_TEST"
  }
}
```

##### 客户端（暂只支持Web类型）

`./CTunnel.Client-win-x64.exe --server wss://127.0.0.1:8888 --token TOKEN_TEST --domain xx.xx.xx --type Web --target https://127.0.0.1:5500`

|参数|描述|
|--|--|
|--server|服务端|
|--token|Token|
|--domain|域名|
|--type|隧道类型，可选值：Web，Tcp，Udp|
|--target|目标服务地址|