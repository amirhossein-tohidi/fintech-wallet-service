using Wallet.Api.Extensions;
using Wallet.Application;
using Wallet.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiServices();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.ConfigureRequestPipeline();

app.Run();
