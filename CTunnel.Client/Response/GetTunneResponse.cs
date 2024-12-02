namespace CTunnel.Client.Response
{
    public class GetTunneResponse
    {
        public string DomainName { get; set; } = string.Empty;

        public string TargetIp { get; set; } = string.Empty;

        public int TargetPort { get; set; }
    }
}
