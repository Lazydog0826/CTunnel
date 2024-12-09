namespace CTunnel.Server
{
    public class AppConfig
    {
        public int ServerPort { get; set; }

        public int HttpPort { get; set; }

        public int HttpsPort { get; set; }

        public string Certificate { get; set; } = string.Empty;

        public string CertificateKey { get; set; } = string.Empty;

        public string Token { get; set; } = string.Empty;
    }
}
