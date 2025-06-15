namespace CTunnel.Share.Expand;

public static class WebStatusResponse
{
    /// <summary>
    /// 返回NotFound响应
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public static async Task ReturnNotFoundAsync(this Stream stream)
    {
        const string response =
            "HTTP/1.1 404 Not Found\r\nContent-Type: text/html\r\n\r\n<html><body><h1>404 Not Found</h1><p>The requested resource was not found.</p></body></html>";
        await stream.WriteAsync(response.ToBytes());
    }
}
