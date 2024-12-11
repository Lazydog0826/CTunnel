using System.IO.Compression;

namespace CTunnel.Share.Expand
{
    public static class GZipExtend
    {
        /// <summary>
        /// 压缩
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static async Task CompressAsync(
            this Memory<byte> source,
            Func<byte[], int, Task> func
        )
        {
            using var outputStream = GlobalStaticConfig.MSManager.GetStream();
            using var gzipStream = new GZipStream(outputStream, CompressionMode.Compress);
            await gzipStream.WriteAsync(source);
            await gzipStream.FlushAsync();
            await BytesExpand.UseBufferAsync(
                (int)outputStream.Length,
                async outputBuffer =>
                {
                    outputStream.Seek(0, SeekOrigin.Begin);
                    var outputBufferCount = await outputStream.ReadAsync(outputBuffer);
                    await func(outputBuffer, outputBufferCount);
                }
            );
        }

        /// <summary>
        /// 解压缩
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static async Task DecompressAsync(
            this Memory<byte> source,
            Func<byte[], int, Task> func
        )
        {
            using var inputStream = GlobalStaticConfig.MSManager.GetStream();
            using var outputStream = GlobalStaticConfig.MSManager.GetStream();
            using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
            await inputStream.WriteAsync(source);
            inputStream.Seek(0, SeekOrigin.Begin);
            await gzipStream.CopyToAsync(outputStream);
            await BytesExpand.UseBufferAsync(
                (int)outputStream.Length,
                async outputBuffer =>
                {
                    outputStream.Seek(0, SeekOrigin.Begin);
                    var outputBufferCount = await outputStream.ReadAsync(outputBuffer);
                    await func(outputBuffer, outputBufferCount);
                }
            );
        }
    }
}
