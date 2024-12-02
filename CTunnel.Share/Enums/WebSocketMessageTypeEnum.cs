namespace CTunnel.Share.Enums
{
    public enum WebSocketMessageTypeEnum
    {
        /// <summary>
        /// 注册隧道
        /// </summary>
        RegisterTunnel = 1,

        /// <summary>
        /// 新请求
        /// </summary>
        NewRequest = 2,

        /// <summary>
        /// 心跳检查
        /// </summary>
        PulseCheck = 3
    }
}
