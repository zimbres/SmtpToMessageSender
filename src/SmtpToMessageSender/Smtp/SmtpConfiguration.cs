namespace SmtpToMessageSender.Smtp;

public class SmtpConfiguration
{
    public string ServerName { get; set; } = string.Empty;
    public bool InsecureConnEnabled { get; set; }
    public int InsecurePort { get; set; }
    public int SecurePort { get; set; }
    public bool SecurePortAuthenticationRequired { get; set; }
    public string CertificateCn { get; set; } = string.Empty;
    public bool SelfSignedCertificate { get; set; }
    public string CertBase64Content { get; set; } = string.Empty;
    public string KeyBase64Content { get; set; } = string.Empty;
    public List<string> AllowedSmtpClients { get; set; } = [];
}
