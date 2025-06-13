# CTunnel

## 介绍

基于.NET8开发的内网穿透

## 使用

### 服务端

```sh
./CTunnel.Server-linux-x64 AppConfig.json
```

```json
{
    "ServerPort": 8888,
    "HttpPort": 80,
    "HttpsPort": 443,
    "Certificate": "cert.pem",
    "CertificateKey": "D:\\Code\\CTunnel\\private.key",
    "Token": "TOKEN_TEST"
}
```

### 客户端

```sh
./CTunnel.Client-win-x64.exe AppConfig.json
```

```json
{
    "ServerUrl": "127.0.0.1:8888",
    "Token": "TOKEN_TEST",
    "DomainName": "xxx",
    "Port": 0,
    // 0=web 1=tcp 2=udp
    "Type": 0,
    "TargetUrl": "http://127.0.0.1:9999"
}
```
