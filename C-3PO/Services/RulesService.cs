using C_3PO.Assets;
using C_3PO.Data.Context;
using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_3PO.Services
{
    public class RulesService : DiscordClientService
    {
        private readonly IServiceProvider _serviceProvider;

        public RulesService(
            DiscordSocketClient client,
            ILogger<DiscordClientService> logger,
            IServiceProvider serviceProvider)
            : base(client, logger)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Client.WaitForReadyAsync(stoppingToken);
            await SyncRules();
        }

        private Task SyncRules()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var configuration = dbContext.Configurations.First();
                    var guild = Client.GetGuild(configuration.Id);
                    var rulesMessage = (await guild.GetTextChannel(configuration.Rules).GetMessagesAsync().FlattenAsync()).First();
                    var conductChannel = guild.GetTextChannel(configuration.Conduct);
                    var conductMessage = (await conductChannel.GetMessagesAsync().FlattenAsync()).FirstOrDefault(x => x.Author.Id == Client.CurrentUser.Id);

                    var rulesEmbed = new EmbedBuilder()
                        .WithTitle("Code of Conduct")
                        .WithDescription(rulesMessage.Content)
                        .WithColor(Colours.Primary)
                        .WithFooter("Last updated at")
                        .WithTimestamp(rulesMessage.EditedTimestamp!.Value)
                        .WithImageUrl(AppAssets.GIFs.TroopersSearching)
                        .Build();
                    
                    if (conductMessage == null)
                        await conductChannel!.SendMessageAsync(embed: rulesEmbed);
                    else
                        await ((IUserMessage)conductMessage!).ModifyAsync(x => x.Embed = rulesEmbed);

                    await Task.Delay(TimeSpan.FromMinutes(5));
                }
            });
            return Task.CompletedTask;
        }
    }
}
