using Wallet.Application;
using Wallet.Infrastructure;
using Wallet.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddWorkerServices(builder.Configuration);

var host = builder.Build();

await host.RunAsync();
