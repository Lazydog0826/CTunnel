using System.Text;
using Newtonsoft.Json;

namespace CTunnel.Share.Expand
{
    public static class BytesExpand
    {
        /// <summary>
        /// 将byte[]转成字符串然后反序列化成对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static T ConvertModel<T>(this byte[] bytes)
        {
            var json = Encoding.UTF8.GetString(bytes);
            try
            {
                return JsonConvert.DeserializeObject<T>(json)!;
            }
            catch
            {
                Log.Write(json, LogType.Error, "ConvertModel");
                throw;
            }
        }
    }
}
