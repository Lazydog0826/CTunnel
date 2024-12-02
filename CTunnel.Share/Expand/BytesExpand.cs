using System.Text;
using Newtonsoft.Json;

namespace CTunnel.Share.Expand
{
    public static class BytesExpand
    {
        public static T ConvertModel<T>(this Memory<byte> memory, int count)
        {
            return memory[..count].ToArray().ConvertModel<T>();
        }

        public static T ConvertModel<T>(this byte[] bytes)
        {
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes))!;
        }
    }
}
