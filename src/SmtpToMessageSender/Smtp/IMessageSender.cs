namespace SmtpToMessageSender.Smtp;

public interface IMessageSender
{
    Task<bool> SendMessageAsync(MailMessage mailMessage);
}
