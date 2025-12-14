var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.SmtpToMessageSender>("smtptomessagesender");

builder.Build().Run();
