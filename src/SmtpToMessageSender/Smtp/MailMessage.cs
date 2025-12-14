namespace SmtpToMessageSender.Smtp;

public class MailMessage
{
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public List<To> To { get; set; } = [];
    public List<Cc> Cc { get; set; } = [];
    public List<Bcc> Bcc { get; set; } = [];
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset Date { get; set; }
}

public class To
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class Cc
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class Bcc
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}
