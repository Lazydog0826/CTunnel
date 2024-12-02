namespace CTunnel.Server
{
    public class AppConfig
    {
        public string Token { get; set; } = string.Empty;

        public bool IsOpenTLS { get; set; }

        /// <summary>
        /// PEM文件路径
        /// </summary>
        public string Certificate { get; set; } = string.Empty;

        /// <summary>
        /// KEY文件路径
        /// </summary>
        public string CertificateKey { get; set; } = string.Empty;
    }
}
