using Newtonsoft.Json;

namespace CTunnel.Share.Model
{
    public class SuccessFailureModel
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        public static bool IsSuccess(string json, out string message)
        {
            var obj = JsonConvert.DeserializeObject<SuccessFailureModel>(json)!;
            message = obj.Message;
            return obj.Success;
        }
    }
}
