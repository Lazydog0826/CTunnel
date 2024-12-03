namespace CTunnel.Share.Model
{
    public class NewRequest
    {
        public string Token { get; set; } = string.Empty;

        public string RequestId { get; set; } = string.Empty;

        public string DomainName { get; set; } = string.Empty;

        public string Host {  get; set; } = string.Empty;
    }
}
