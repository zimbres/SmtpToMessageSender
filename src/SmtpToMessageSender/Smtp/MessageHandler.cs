namespace SmtpToMessageSender.Smtp;

public sealed class MessageHandler : MessageStore
{
    private readonly ILogger<MessageHandler> _logger;
    private readonly IMessageSender _messageSender;
    private readonly ActivitySource _activitySource;
    private readonly SmtpConfiguration _smtpConfiguration;

    public MessageHandler(IMessageSender messageSender, ActivitySource activitySource, ILogger<MessageHandler> logger, IConfiguration configuration)
    {
        _messageSender = messageSender;
        _activitySource = activitySource;
        _logger = logger;
        _smtpConfiguration = configuration.GetSection("SmtpConfiguration").Get<SmtpConfiguration>()!;
    }

    public override async Task<SmtpResponse> SaveAsync(ISessionContext context, IMessageTransaction transaction,
        ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
    {
        Metrics.SmtpMessagesReceived.Add(1);
        using var activity = _activitySource.StartActivity("MessageHandler.SaveMessage", ActivityKind.Server);

        activity?.SetTag("protocol", "smtp");
        activity?.SetTag("smtp.session.id", context.SessionId);
        activity?.SetTag("smtp.message.size", buffer.Length);
        if (context.Properties.TryGetValue("EndpointListener:RemoteEndPoint", out var endpointValue) && endpointValue is IPEndPoint ipEndPoint)
        {
            IPAddress ipAddress = ipEndPoint.Address;
            using var checkIp = _activitySource.StartActivity("MessageHandler.CheckClientIp", ActivityKind.Internal);
            checkIp?.SetTag("smtp.client.ip", ipAddress);

            var AllowedSmtpClients = _smtpConfiguration.AllowedSmtpClients;

            if (AllowedSmtpClients.Count != 0 && !AllowedSmtpClients.Any(a => IpHelper.IpMatches(a, ipAddress)))
            {
                checkIp?.SetTag("smtp.client.allowed", false);
                _logger.LogError("Connection from an IP not allowed: {IpAddress}", ipAddress);
                return SmtpResponse.TransactionFailed;
            }
            checkIp?.SetTag("smtp.client.allowed", true);
        }

        try
        {
            await using var stream = new MemoryStream();

            var position = buffer.GetPosition(0);
            while (buffer.TryGet(ref position, out var memory))
            {
                await stream.WriteAsync(memory, cancellationToken);
            }

            stream.Position = 0;

            using var parseActivity = _activitySource.StartActivity("MessageHandler.ParseMime", ActivityKind.Internal);
            var mime = await MimeMessage.LoadAsync(stream, cancellationToken);

            parseActivity?.SetTag("email.subject", mime.Subject);
            parseActivity?.SetTag("email.to.count", mime.To.Mailboxes.Count());
            parseActivity?.SetTag("email.cc.count", mime.Cc.Mailboxes.Count());
            parseActivity?.SetTag("email.bcc.count", mime.Bcc.Mailboxes.Count());

            var message = new MailMessage
            {
                FromName = mime.From.Mailboxes.First().Name,
                FromAddress = mime.From.Mailboxes.First().Address,
                To = [.. mime.To.Mailboxes.Select(m => new To { Address = m.Address, Name = m.Name })],
                Cc = [.. mime.Cc.Mailboxes.Select(m => new Cc { Address = m.Address, Name = m.Name })],
                Bcc = [.. mime.Bcc.Mailboxes.Select(m => new Bcc { Address = m.Address, Name = m.Name })],
                Subject = mime.Subject,
                Message = mime.TextBody,
                Date = mime.Date
            };

            var sent = await _messageSender.SendMessageAsync(message);

            activity?.SetTag("smtp.send.success", sent);

            if (sent)
            {
                Metrics.SmtpMessagesForwarded.Add(1);
                activity?.SetStatus(ActivityStatusCode.Ok);
                return SmtpResponse.Ok;
            }

            Metrics.SmtpMessagesFailed.Add(1);
            activity?.SetStatus(ActivityStatusCode.Error, "Failed to forward message");
            return SmtpResponse.MailboxUnavailable;
        }
        catch (OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Operation canceled");
            Metrics.SmtpMessagesFailed.Add(1);
            throw;
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            Metrics.SmtpMessagesFailed.Add(1);
            throw;
        }
    }
}
