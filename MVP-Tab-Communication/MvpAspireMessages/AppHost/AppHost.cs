var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.MvpAspireMessages_Api>("api")
    .WithHttpEndpoint(port: 5305, name: "http", isProxied: false)
    .WithEnvironment("ASPNETCORE_URLS", "http://127.0.0.1:5305")
    .WithEnvironment("ASPNETCORE_HTTPS_PORT", "");

builder.AddProject<Projects.MvpAspireMessages_Web>("web")
    .WithReference(api)
    .WaitFor(api)
    .WithHttpEndpoint(port: 5300, name: "http", isProxied: false)
    .WithEnvironment("ASPNETCORE_URLS", "http://127.0.0.1:5300")
    .WithEnvironment("ASPNETCORE_HTTPS_PORT", "")
    .WithEnvironment("MessagesApi__BaseUrl", "http://127.0.0.1:5305");

builder.Build().Run();
