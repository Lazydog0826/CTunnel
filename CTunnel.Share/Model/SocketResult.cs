using Newtonsoft.Json;

namespace CTunnel.Share.Model
{
    public class SocketResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 是否成功
        /// </summary>
        /// <param name="json"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool IsSuccess(string json, out string message)
        {
            var obj = JsonConvert.DeserializeObject<SocketResult>(json)!;
            message = obj.Message;
            return obj.Success;
        }
    }
}
