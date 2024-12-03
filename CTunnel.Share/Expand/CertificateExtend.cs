using System.Security.Cryptography.X509Certificates;

namespace CTunnel.Share.Expand
{
    public static class CertificateExtend
    {
        public static X509Certificate2 LoadPem(string pem, string key)
        {
            var password = Guid.NewGuid().ToString("N");
            var certPem = File.ReadAllText(pem);
            var keyPem = File.ReadAllText(key);
            var x509Certificate2 = X509Certificate2.CreateFromPem(certPem, keyPem);
            return new X509Certificate2(
                x509Certificate2.Export(X509ContentType.Pfx, password),
                password
            );
        }
    }
}
