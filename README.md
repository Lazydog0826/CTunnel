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
        // 服务端端口
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

##### 客户端

`./CTunnel.Client-win-x64.exe --server wss://127.0.0.1:8888 --token TOKEN_TEST --domain xx.xx.xx --type Web --target https://127.0.0.1:5500`

|参数|描述|示例|
|--|--|--|
|--server|服务端地址|wss://127.0.0.1:8888|
|--token|Token|TOKEN_TEST|
|--domain|域名，Web类型需要指定域名，Tcp与Udp则不需要指定|a.test.com|
|--port|监听的端口，服务端对于隧道监听的端口，Tcp与Udp需要指定，Web类型则不需要|5500|
|--type|隧道类型，可选值：Web，Tcp，Udp|Web|
|--target|目标服务地址|http://127.0.0.1:8080|
