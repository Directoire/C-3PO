using C_3PO.Common;
using C_3PO.Data.Context;
using C_3PO.Handlers;
using C_3PO.Services;
using Discord;
using Discord.Addons.Hosting;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting host");

#if DEBUG
    var appConfiguration = JsonConvert.DeserializeObject<AppConfiguration>(File.ReadAllText("appsettings.Development.json"))!;
#else
    var appConfiguration = JsonConvert.DeserializeObject<AppConfiguration>(File.ReadAllText("appsettings.json"))!;
#endif

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

            config.Token = appConfiguration.Token;
        })
        .UseInteractionService((context, config) =>
        {
            config.LogLevel = LogSeverity.Info;
            config.UseCompiledLambda = true;
            config.DefaultRunMode = RunMode.Async;
        })
        .ConfigureServices((context, services) =>
        {
            var serverVersion = new MySqlServerVersion(new Version(8, 0, 26));
            var dbContextOptionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
                .UseMySql(appConfiguration.Database, serverVersion);

            using (var dbContext = new AppDbContext(dbContextOptionsBuilder.Options))
            {
                dbContext.Database.Migrate();
            }

            services
                .AddHostedService<InteractionHandler>()
                .AddHostedService<UserJoinedHandler>()
                .AddHostedService<UserLeftHandler>()
                .AddHostedService<ButtonExecutedHandler>()
                .AddHostedService<FeedsService>()
                .AddHostedService<RulesService>()
                .AddHostedService<BansService>()
                .AddHostedService<StatusService>()
                .AddHostedService<MessageDeletedHandler>()
                .AddHostedService<MessageUpdatedHandler>()
                .AddHostedService<MessageReceivedHandler>()
                .AddHostedService<LoadingBayService>()
                .AddHostedService<HeartbeatService>()
                .AddSingleton<LogsService>()
                .AddSingleton<OnboardingService>()
                .AddHttpClient()
                .AddDbContext<AppDbContext>(options => 
                    options
                    .UseMySql(appConfiguration.Database, serverVersion))
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