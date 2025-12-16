namespace SmtpToMessageSender.LogicApp;

public class LogicAppSender : IMessageSender
{
    private readonly ILogger<LogicAppSender> _logger;
    private readonly HttpClient _httpClient;
    private readonly HttpConfiguration _httpConfiguration;
    private readonly ActivitySource _activitySource;

    public LogicAppSender(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<LogicAppSender> logger, ActivitySource activitySource)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _httpConfiguration = configuration.GetSection("HttpConfiguration").Get<HttpConfiguration>()!;
        _activitySource = activitySource;
    }

    public async Task<bool> SendMessageAsync(MailMessage mailMessage)
    {
        using var activity = _activitySource.StartActivity("LogicAppSender.SendMessageAsync", ActivityKind.Internal);

        activity?.SetTag("messaging.system", "email");
        activity?.SetTag("messaging.destination", "azure.logicapp");
        activity?.SetTag("email.to.count", mailMessage.To.Count);
        activity?.SetTag("http.url", new Uri(_httpConfiguration.Url).GetLeftPart(UriPartial.Authority));

        try
        {
            var json = JsonSerializer.Serialize(new MessageBody
            {
                EmailTo = string.Join(";", mailMessage.To.Select(s => s.Address)),
                EmailSubject = mailMessage.Subject,
                EmailBody = mailMessage.Message
            });

            var request = new HttpRequestMessage(HttpMethod.Post, _httpConfiguration.Url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            var sw = Stopwatch.StartNew();
            var response = await _httpClient.SendAsync(request);
            sw.Stop();
            Metrics.MessageSenderSendDuration.Record(sw.Elapsed.TotalMilliseconds);

            activity?.SetTag("http.status_code", (int)response.StatusCode);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Metrics.MessageSenderSendSuccess.Add(1);
                activity?.SetStatus(ActivityStatusCode.Ok);
                return true;
            }

            activity?.AddEvent(new ActivityEvent("Logic App returned non-OK status code", tags: new ActivityTagsCollection
            {
                ["http.status_code"] = response.StatusCode,
                ["http.reason_phrase"] = response.ReasonPhrase
            }));
            activity?.SetStatus(ActivityStatusCode.Error, $"Unexpected status code {response.StatusCode}");
            Metrics.MessageSenderSendFailure.Add(1);
            return false;
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            Metrics.MessageSenderSendFailure.Add(1);
            _logger.LogError(ex, "Error sending message to Logic App");
            return false;
        }
    }
}
