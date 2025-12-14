namespace SmtpToMessageSender.Accounts;

public class Accounts : List<Account> { }

public class Account
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
