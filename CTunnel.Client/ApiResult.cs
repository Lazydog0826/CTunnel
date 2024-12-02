using System.Net;

namespace CTunnel.Client
{
    public class ApiResult<T>(HttpStatusCode code, string message, T data)
    {
        public HttpStatusCode Code { get; set; } = code;

        public string Message { get; set; } = message;

        public T Data { get; set; } = data;
    }
}
