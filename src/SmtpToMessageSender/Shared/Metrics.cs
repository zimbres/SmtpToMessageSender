namespace SmtpToMessageSender.Shared;

public static class Metrics
{
    public const string MeterName = "SmtpToMessageSender";

    public static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> SmtpAuthenticationSuccessful = Meter.CreateCounter<long>("smtp.authentication.successful", description: "Number of SMTP authentication successful");

    public static readonly Counter<long> SmtpAuthenticationFailed = Meter.CreateCounter<long>("smtp.authentication.failed", description: "Number of SMTP authentication failed");

    public static readonly Counter<long> SmtpMessagesReceived = Meter.CreateCounter<long>("smtp.messages.received", description: "Number of SMTP messages received");

    public static readonly Counter<long> SmtpMessagesForwarded = Meter.CreateCounter<long>("smtp.messages.forwarded", description: "Number of SMTP messages forwarded to Message Sender");

    public static readonly Counter<long> SmtpMessagesFailed = Meter.CreateCounter<long>("smtp.messages.failed", description: "Number of SMTP messages that failed processing");

    public static readonly Counter<long> MessageSenderSendSuccess = Meter.CreateCounter<long>("messagesender.send.success", description: "Number of messages successfully sent");

    public static readonly Counter<long> MessageSenderSendFailure = Meter.CreateCounter<long>("messagesender.send.failure", description: "Number of messages that failed to send");

    public static readonly Histogram<double> MessageSenderSendDuration = Meter.CreateHistogram<double>("messagesender.send.duration", unit: "ms", description: "Time spent by Message Sender");
}
