namespace SmtpToMessageSender;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly SmtpServer.SmtpServer _smtpServer;

    public Worker(ILogger<Worker> logger, SmtpServer.SmtpServer smtpServer)
    {
        _logger = logger;
        _smtpServer = smtpServer;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning("App version: {version}", Assembly.GetExecutingAssembly().GetName().Version?.ToString());
        }

        try
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            return _smtpServer.StartAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while starting the worker.");
        }
        return Task.CompletedTask;
    }
}
