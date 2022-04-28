using C_3PO.Assets;
using C_3PO.Common;
using C_3PO.Services;
using Discord;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace C_3PO.Handlers
{
    public class MessageReceivedHandler : DiscordClientService
    {
        private readonly LogsService _logsService;
        private readonly IServiceProvider _serviceProvider;
        private readonly AppConfiguration _configuration;

        public MessageReceivedHandler(
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
            Client.MessageReceived += Client_MessageReceived;
            return Task.CompletedTask;
        }

        private Task Client_MessageReceived(SocketMessage msg)
        {
            if (msg.Channel.Id != _configuration.Channels.Support)
                return Task.CompletedTask;

            if (msg.Channel is not SocketTextChannel channel)
                return Task.CompletedTask;

            Task.Run(async () =>
            {
                var thread = await channel.CreateThreadAsync(msg.Author.Username, autoArchiveDuration: ThreadArchiveDuration.ThreeDays, message: msg);

                var embed = new EmbedBuilder()
                    .WithTitle("Hello there!")
                    .WithDescription($"Welcome to your thread {msg.Author.Mention}. Please take a look at the rules while other members are rushing to answer your question.")
                    .AddField("Rules", "1. **Remain patient** - do not reach out to others in private to answer your question.\n2. **Use a pastebin** - use websites like pastebin.com to share your code, do not paste large portions of code in this thread.\n3. **If you don't understand, ask questions** - pretending to understand can make it harder for the other party to help you.")
                    .WithColor(Colours.Primary)
                    .WithImageUrl(AppAssets.GIFs.HanSoloConfused)
                    .Build();

                await thread.SendMessageAsync(embed: embed);
            });
            return Task.CompletedTask;
        }
    }
}
