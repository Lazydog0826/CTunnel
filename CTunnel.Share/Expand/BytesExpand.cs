using System.Text;
using Newtonsoft.Json;

namespace CTunnel.Share.Expand
{
    public static class BytesExpand
    {
        public static T ConvertModel<T>(this Span<byte> bytes)
        {
            var json = Encoding.UTF8.GetString(bytes);
            try
            {
                return JsonConvert.DeserializeObject<T>(json)!;
            }
            catch
            {
                Log.Write($"消息读取失败 {json}");
                throw;
            }
        }
    }
}
