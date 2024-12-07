namespace CTunnel.Share.Expand
{
    public static class TLSExtend
    {
        public static bool IsNeedTLS(this UriBuilder uriBuilder)
        {
            return uriBuilder.Scheme is "https" or "wss";
        }
    }
}
