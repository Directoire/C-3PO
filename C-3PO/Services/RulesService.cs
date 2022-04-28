using C_3PO.Assets;
using C_3PO.Common;
using C_3PO.Data.Context;
using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace C_3PO.Services
{
    public class RulesService : DiscordClientService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly AppConfiguration _configuration;

        public RulesService(
            DiscordSocketClient client,
            ILogger<DiscordClientService> logger,
            IServiceProvider serviceProvider,
            AppConfiguration configuration)
            : base(client, logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
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

                    var guild = Client.GetGuild(_configuration.Guild);
                    var rulesChannel = guild.GetTextChannel(_configuration.Channels.Rules);
                    var rulesMessage = (await rulesChannel.GetMessagesAsync().FlattenAsync()).FirstOrDefault();
                    if (rulesMessage == null)
                    {
                        Logger.LogWarning($"There could not be found any messages in #{rulesChannel.Name}, rules not updated");
                        return;
                    }

                    var conductChannel = guild.GetTextChannel(_configuration.Channels.Conduct);
                    var conductMessage = (await conductChannel.GetMessagesAsync().FlattenAsync()).FirstOrDefault(x => x.Author.Id == Client.CurrentUser.Id);

                    var rulesEmbed = new EmbedBuilder()
                        .WithTitle("Code of Conduct")
                        .WithDescription(rulesMessage.Content)
                        .WithColor(Colours.Primary)
                        .WithFooter("Last updated at")
                        .WithTimestamp(rulesMessage.EditedTimestamp.HasValue ? rulesMessage.EditedTimestamp.Value : DateTimeOffset.Now)
                        .WithImageUrl(AppAssets.GIFs.TroopersSearching)
                        .Build();

                    if (conductMessage == null)
                    {
                        await conductChannel!.SendMessageAsync(embed: rulesEmbed);
                    }
                    else
                    {
                        await ((IUserMessage)conductMessage!).ModifyAsync(x => x.Embed = rulesEmbed);
                    }
                        

                    await Task.Delay(TimeSpan.FromMinutes(5));
                }
            });
            return Task.CompletedTask;
        }
    }
}
