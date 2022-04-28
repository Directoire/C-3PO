using C_3PO.Assets;
using C_3PO.Common;
using C_3PO.Data.Context;
using C_3PO.Services;
using Discord;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_3PO.Handlers
{
    public class MessageUpdatedHandler : DiscordClientService
    {
        private readonly LogsService _logsService;
        private readonly IServiceProvider _serviceProvider;
        private readonly AppConfiguration _configuration;

        public MessageUpdatedHandler(
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
            Client.MessageUpdated += Client_MessageUpdated;
            return Task.CompletedTask;
        }

        private Task Client_MessageUpdated(Cacheable<IMessage, ulong> oldMessage, SocketMessage newMessage, ISocketMessageChannel channel)
        {
            Task.Run(async () =>
            {
                if (newMessage.Author.Id == Client.CurrentUser.Id)
                    return;

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                if (channel.Id == _configuration.Channels.Logs)
                    return;

                if (oldMessage.HasValue && oldMessage.Value.Content == newMessage.Content)
                    return;

                var embed = new EmbedBuilder()
                    .WithDescription($"Message by {newMessage.Author.Mention} in <#{channel.Id}> edited.")
                    .WithColor(Colours.Primary)
                    .AddField("From", oldMessage.Value.Content.Truncate(250))
                    .AddField("To", newMessage.Content.Truncate(250))
                    .Build();

                await _logsService.Log(embed);

            });
            return Task.CompletedTask;
        }
    }
}