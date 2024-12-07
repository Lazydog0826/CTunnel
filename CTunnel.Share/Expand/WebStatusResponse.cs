using System.Text;

namespace CTunnel.Share.Expand
{
    public static class WebStatusResponse
    {
        /// <summary>
        /// 返回模板html响应
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task ReturnTemplateHtmlAsync(this Stream stream, string message)
        {
            var responseContent = $"<h1>{message}</h1>";
            var httpResponse =
                $"HTTP/1.1 200 OK\r\n"
                + $"Content-Type: text/html; charset=utf-8\r\n"
                + $"Content-Length: {responseContent.Length}\r\n"
                + $"\r\n"
                + responseContent;
            await stream.WriteAsync(Encoding.UTF8.GetBytes(httpResponse));
        }
    }
}
