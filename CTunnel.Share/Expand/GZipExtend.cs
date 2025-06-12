using System.Buffers;
using System.IO.Compression;
using Microsoft.IO;

namespace CTunnel.Share.Expand;

public static class GZipExtend
{
    /// <summary>
    /// 压缩
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static async Task<RecyclableMemoryStream> CompressAsync(this Memory<byte> source)
    {
        // outputStream将返回给调用方
        var outputStream = GlobalStaticConfig.MsManager.GetStream();
        await using var gzipStream = new GZipStream(outputStream, CompressionMode.Compress);
        await gzipStream.WriteAsync(source);
        await gzipStream.FlushAsync();
        outputStream.Seek(0, SeekOrigin.Begin);
        return outputStream;
    }

    /// <summary>
    /// 解压缩
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public static async Task<RecyclableMemoryStream> DecompressAsync(this Memory<byte> source)
    {
        // outputStream将返回给调用方
        var outputStream = GlobalStaticConfig.MsManager.GetStream();
        await using var inputStream = GlobalStaticConfig.MsManager.GetStream();
        await using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
        await inputStream.WriteAsync(source);
        inputStream.Seek(0, SeekOrigin.Begin);
        await gzipStream.CopyToAsync(outputStream);
        outputStream.Seek(0, SeekOrigin.Begin);
        return outputStream;
    }
}
