using CTunnel.Share.Enums;

namespace CTunnel.Share.Model
{
    public class SocketTypeMessage
    {
        /// <summary>
        /// 消息类型
        /// </summary>
        public WebSocketMessageTypeEnum MessageType { get; set; }

        /// <summary>
        /// JSON格式数据
        /// </summary>
        public string JsonData { get; set; } = string.Empty;
    }
}
