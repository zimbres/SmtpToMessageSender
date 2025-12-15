var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddSingleton(sp => new ActivitySource(builder.Environment.ApplicationName));
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton(builder.Configuration.GetSection("Accounts").Get<Accounts>()!);
builder.Services.AddTransient<IMessageStore, MessageHandler>();
builder.Services.AddTransient<IUserAuthenticator, SmtpToMessageSender.Accounts.UserAuthenticator>();
builder.Services.AddTransient<IUserAuthenticatorFactory, SmtpToMessageSender.Accounts.UserAuthenticator>();
builder.Services.AddSingleton<CertificateHandler>();
builder.Services.AddSingleton(provider =>
{
    var smtpConfig = builder.Configuration.GetSection("SmtpConfiguration").Get<SmtpConfiguration>()!;

    var optionsBuilder = new SmtpServerOptionsBuilder().ServerName(smtpConfig.ServerName);

    if (smtpConfig.InsecureConnEnabled)
    {
        optionsBuilder.Endpoint(builder => builder.Port(smtpConfig.InsecurePort).IsSecure(false));
    }

    optionsBuilder.Endpoint(endpointBuilder =>
    {
        endpointBuilder.Port(smtpConfig.SecurePort)
            .IsSecure(true)
            .AllowUnsecureAuthentication(false)
            .SupportedSslProtocols(System.Security.Authentication.SslProtocols.Tls13 | System.Security.Authentication.SslProtocols.Tls12)
            .Certificate(provider.GetRequiredService<CertificateHandler>().GetCertificate());

        if (smtpConfig.SecurePortAuthenticationRequired)
        {
            endpointBuilder.AuthenticationRequired();
        }
    });

    var options = optionsBuilder.Build();

    return new SmtpServer.SmtpServer(options, provider.GetRequiredService<IServiceProvider>());
});
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IMessageSender, LogicAppSender>();

var host = builder.Build();
host.Run();
