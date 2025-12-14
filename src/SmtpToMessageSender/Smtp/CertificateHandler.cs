namespace SmtpToMessageSender.Smtp;

public class CertificateHandler
{
    private readonly SmtpConfiguration _smtpConfiguration;

    public CertificateHandler(IConfiguration configuration)
    {
        _smtpConfiguration = configuration.GetSection("SmtpConfiguration").Get<SmtpConfiguration>()!;
    }

    public X509Certificate2 GetCertificate()
    {
        if (!_smtpConfiguration.SelfSignedCertificate)
        {
            var certBase64 = _smtpConfiguration.CertBase64Content;
            var keyBase64 = _smtpConfiguration.KeyBase64Content;

            var certPem = Encoding.UTF8.GetString(Convert.FromBase64String(certBase64));
            var keyPem = Encoding.UTF8.GetString(Convert.FromBase64String(keyBase64));

            var tempCert = X509Certificate2.CreateFromPem(certPem, keyPem);

            var pfxBytes = tempCert.Export(X509ContentType.Pfx);

            return X509CertificateLoader.LoadPkcs12(pfxBytes, password: string.Empty, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);
        }

        var cn = _smtpConfiguration.CertificateCn;

        using var rsa = RSA.Create(4096);

        var request = new CertificateRequest($"CN={cn}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.7.3.1")], critical: false));

        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, critical: true));

        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName(cn);
        request.CertificateExtensions.Add(sanBuilder.Build());

        var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));

        return X509CertificateLoader.LoadPkcs12(cert.Export(X509ContentType.Pfx), password: string.Empty, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);
    }
}
