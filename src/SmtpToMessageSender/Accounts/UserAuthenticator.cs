namespace SmtpToMessageSender.Accounts;

public class UserAuthenticator : IUserAuthenticator, IUserAuthenticatorFactory
{
    private readonly ILogger<UserAuthenticator> _logger;
    private readonly Accounts? _accounts;

    public UserAuthenticator()
    {
        _logger = null!;
    }

    public UserAuthenticator(ILogger<UserAuthenticator> logger, Accounts accounts)
    {
        _logger = logger;
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
    }

    public IUserAuthenticator CreateInstance(ISessionContext context)
    {
        return new UserAuthenticator(_logger, _accounts!);
    }

    public Task<bool> AuthenticateAsync(ISessionContext context, string user, string password, CancellationToken token)
    {
        if (_accounts == null || !_accounts.Any())
        {
            _logger.LogError("Authentication failed: No accounts configured.");
            return Task.FromResult(false);
        }

        var isAuthenticated = _accounts.Any(account => string.Equals(account.Username, user, StringComparison.OrdinalIgnoreCase) && account.Password == password);

        if (!isAuthenticated)
        {
            Metrics.SmtpAuthenticationFailed.Add(1);
            if (context.Properties.TryGetValue("EndpointListener:RemoteEndPoint", out var endpointValue) && endpointValue is IPEndPoint ipEndPoint)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning("Authentication failed: Invalid credentials for user '{User}'. Remote IP: {IpAddress}", user, ipEndPoint.Address);
                }
            }
            else
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning("Authentication failed: Invalid credentials for user '{User}'. Remote IP could not be determined.", user);
                }
            }
        }
        else
        {
            Metrics.SmtpAuthenticationSuccessful.Add(1);
        }
        return Task.FromResult(isAuthenticated);
    }
}
