using System.Text;
using Newtonsoft.Json;

namespace CTunnel.Share.Expand;

public static class BytesExpand
{
    /// <summary>
    /// 将byte[]转成字符串然后反序列化成对象
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="memory"></param>
    /// <returns></returns>
    public static T ConvertModel<T>(this Memory<byte> memory)
    {
        var json = Encoding.UTF8.GetString(memory.Span);
        return JsonConvert.DeserializeObject<T>(json) ?? throw new Exception("json转换失败");
    }

    public static byte[] ToBytes(this string str)
    {
        return Encoding.UTF8.GetBytes(str);
    }
}
