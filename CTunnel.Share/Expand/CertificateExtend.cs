using System.Security.Cryptography.X509Certificates;

namespace CTunnel.Share.Expand;

public static class CertificateExtend
{
    /// <summary>
    /// 读取PEM,KEY类型证书，生成随机密码，并转成Pfx格式
    /// </summary>
    /// <param name="pem"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static X509Certificate2 LoadPem(string pem, string key)
    {
        var certPem = File.ReadAllText(pem);
        var keyPem = File.ReadAllText(key);
        return X509Certificate2.CreateFromPem(certPem, keyPem);
    }
}
