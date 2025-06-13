namespace CTunnel.Share.Expand;

public static class TlsExtend
{
    /// <summary>
    /// 判断协议是否需要TLS/SSL握手
    /// </summary>
    /// <param name="uriBuilder"></param>
    /// <returns></returns>
    public static bool IsNeedTls(this UriBuilder uriBuilder)
    {
        return uriBuilder.Scheme is "https" or "wss";
    }
}
