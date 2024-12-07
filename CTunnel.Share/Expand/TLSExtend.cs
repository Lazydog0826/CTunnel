namespace CTunnel.Share.Expand
{
    public static class TLSExtend
    {
        /// <summary>
        /// 判断协议是否需要TLS/SSL握手
        /// </summary>
        /// <param name="uriBuilder"></param>
        /// <returns></returns>
        public static bool IsNeedTLS(this UriBuilder uriBuilder)
        {
            return uriBuilder.Scheme is "https" or "wss";
        }
    }
}
