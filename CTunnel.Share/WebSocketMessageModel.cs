namespace CTunnel.Share;

public class WebSocketMessageModel
{
    public WebSocketMessageTypeEnum MessageType { get; set; }

    public string Data { get; set; } = string.Empty;
}

public enum WebSocketMessageTypeEnum
{
    ConnectionSuccessful,
    ConnectionFail,
    NewRequest
}
