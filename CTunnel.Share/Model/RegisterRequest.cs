namespace CTunnel.Share.Model;

public class RegisterRequest
{
    public string Token { get; set; } = string.Empty;

    public string TunnelKey { get; set; } = string.Empty;

    public string RequestId { get; set; } = string.Empty;
}
