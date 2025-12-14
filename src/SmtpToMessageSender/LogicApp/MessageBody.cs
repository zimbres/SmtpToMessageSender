namespace SmtpToMessageSender.LogicApp;

internal class MessageBody
{
    [JsonPropertyName("email_to")]
    public string EmailTo { get; set; } = string.Empty;

    [JsonPropertyName("email_subject")]
    public string EmailSubject { get; set; } = string.Empty;

    [JsonPropertyName("email_body")]
    public string EmailBody { get; set; } = string.Empty;
}
