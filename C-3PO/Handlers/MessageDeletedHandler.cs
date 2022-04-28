using C_3PO.Assets;
using C_3PO.Common;
using C_3PO.Data.Context;
using C_3PO.Services;
using Discord;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace C_3PO.Handlers
{
    public class MessageDeletedHandler : DiscordClientService
    {
        private readonly LogsService _logsService;
        private readonly IServiceProvider _serviceProvider;
        private readonly AppConfiguration _configuration;

        public MessageDeletedHandler(
            DiscordSocketClient client,
            ILogger<DiscordClientService> logger,
            LogsService logsService,
            IServiceProvider serviceProvider,
            AppConfiguration configuration)
            : base(client, logger)
        {
            _logsService = logsService;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Client.MessageDeleted += Client_MessageDeleted;
            return Task.CompletedTask;
        }

        private Task Client_MessageDeleted(Cacheable<IMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel)
        {
            Task.Run(async () =>
            {
                if (message.HasValue && message.Value.Author.Id == Client.CurrentUser.Id)
                    return;

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                if (channel.HasValue && channel.Value.Id == _configuration.Channels.Logs)
                    return;

                await _logsService.Log($"Message by {message.Value.Author.Mention} deleted in <#{channel.Id}>.{(message.HasValue ? $"\n\n{message.Value.Content.Truncate(500)}" : "")}");
            });
            return Task.CompletedTask;
        }
    }
}
