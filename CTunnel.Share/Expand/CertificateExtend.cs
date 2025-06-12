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
        var password = Guid.NewGuid().ToString("N");
        var x509Certificate2 = X509Certificate2.CreateFromPemFile(pem, key);
        return X509CertificateLoader.LoadPkcs12(
            x509Certificate2.Export(X509ContentType.Pfx, password),
            password
        );
    }
}
