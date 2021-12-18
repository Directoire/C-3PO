using Discord;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using C_3PO.Services;
using Discord.Interactions;
using Newtonsoft.Json;
using C_3PO.Common;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting host");

    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureDiscordHost((context, config) =>
        {
            config.SocketConfig = new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                AlwaysDownloadUsers = true,
                MessageCacheSize = 200,
                LogGatewayIntentWarnings = false,
                GatewayIntents = GatewayIntents.All
            };

            config.Token = context.Configuration["Token"];
        })
        .UseInteractionService((context, config) =>
        {
            config.LogLevel = LogSeverity.Info;
            config.UseCompiledLambda = true;
            config.DefaultRunMode = RunMode.Async;
        })
        .ConfigureServices((context, services) =>
        {
            var appConfiguration = new AppConfiguration(context.Configuration);

            services
            .AddHostedService<InteractionHandler>()
            .AddHostedService<OnboardingHandler>()
            .AddSingleton(appConfiguration);
        }).Build();

    await host.RunAsync();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}