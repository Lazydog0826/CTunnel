using System.Text;

namespace CTunnel.Share.Expand
{
    public static class WebStatusResponse
    {
        /// <summary>
        /// 返回NotFound响应
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task ReturnNotFoundAsync(this Stream stream)
        {
            var response =
                "HTTP/1.1 404 Not Found\r\n"
                + "Content-Type: text/html\r\n"
                + "\r\n"
                + "<html><body><h1>404 Not Found</h1><p>The requested resource was not found.</p></body></html>";
            await stream.WriteAsync(Encoding.UTF8.GetBytes(response));
        }
    }
}
