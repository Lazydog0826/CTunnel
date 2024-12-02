namespace CTunnel.Server
{
    public class AppConfig
    {
        public string Token { get; set; } = string.Empty;

        public bool IsOpenTLS { get; set; }

        public string Certificate { get; set; } = string.Empty;

        public string CertificatePassword { get; set; } = string.Empty;
    }
}
