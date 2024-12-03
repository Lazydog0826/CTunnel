using System.Text;

namespace CTunnel.Share.Expand
{
    public static class WebStatusResponse
    {
        public static async Task ReturnTemplateHtmlAsync(this Stream stream, string message)
        {
            var responseContent = $"<html><body>{message}</body></html>";
            var httpResponse =
                $"HTTP/1.1 200 OK\r\n"
                + $"Content-Type: text/html; charset=utf-8\r\n"
                + $"Content-Length: {responseContent.Length}\r\n"
                + $"\r\n"
                + $"{responseContent}";

            var responseBytes = Encoding.UTF8.GetBytes(httpResponse);
            await stream.WriteAsync(responseBytes);
        }
    }
}
