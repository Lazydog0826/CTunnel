using System.Buffers;
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
        /// <param name="memory"></param>
        /// <returns></returns>
        public static T ConvertModel<T>(this Memory<byte> memory)
        {
            var json = Encoding.UTF8.GetString(memory.Span);
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

        /// <summary>
        /// Buffer扩展
        /// </summary>
        /// <param name="size"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public static async Task UseBufferAsync(int size, Func<byte[], Task> func)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(size);
            try
            {
                await func(buffer);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
