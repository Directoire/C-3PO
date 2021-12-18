using C_3PO.Common;
using Discord;
using Discord.Addons.Hosting;
using Discord.Interactions;
using Discord.Webhook;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace C_3PO.Services
{
    internal class OnboardingHandler : DiscordClientService
    {
        private readonly IServiceProvider _provider;
        private readonly InteractionService _interactionService;
        private readonly AppConfiguration _configuration;

        public OnboardingHandler(DiscordSocketClient client, ILogger<DiscordClientService> logger, IServiceProvider provider, InteractionService interactionService, AppConfiguration configuration)
            : base(client, logger)
        {
            _provider = provider;
            _interactionService = interactionService;
            _configuration = configuration;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Client.UserJoined += HandleUserJoined;
            return Task.CompletedTask;
        }

        private async Task HandleUserJoined(SocketGuildUser user)
        {
            if (user.Guild.Id != _configuration.Guild)
                return;

            await Task.Run(async () =>
            {
                var permissionOverwrites = new List<Overwrite>()
                {
                    new Overwrite(user.Guild.EveryoneRole.Id, PermissionTarget.Role, new OverwritePermissions(viewChannel: PermValue.Deny)),
                    new Overwrite(user.Id, PermissionTarget.User, new OverwritePermissions(viewChannel: PermValue.Allow))
                };

                var textChannel = await user.Guild.CreateTextChannelAsync(user.Username + "-onboarding", t =>
                {
                    t.CategoryId = _configuration.Categories.OuterRim;
                    t.PermissionOverwrites = permissionOverwrites;
                });

                await textChannel.TriggerTypingAsync();
                await Task.Delay(1000);
                await textChannel.SendMessageAsync("Hello there, stranger! I can't seem to identify you, you must be new. We've put you in Docking Bay 327 to get some things sorted. I've dispatched some troopers.");
                await textChannel.SendMessageAsync("https://media.giphy.com/media/xTiIzsz8RsfugNsg6s/giphy.gif");

                using var httpClient = new HttpClient();
                var avatar = await httpClient.GetStreamAsync(user.GetAvatarUrl());
                var webhookClient = new DiscordWebhookClient(await textChannel.CreateWebhookAsync(user.Username, avatar));

                await Task.Delay(1000);
                var components = new ComponentBuilder()
                    .WithButton("Cooperate", "cooperate", ButtonStyle.Success)
                    .WithButton("Try to fight your way in", "fight", ButtonStyle.Danger)
                    .Build();

                await textChannel.SendMessageAsync("The troopers have arrived at your ship. What do you do?", components: components);

                await Task.Delay(1000);
                await webhookClient.SendMessageAsync("*Shows identification*");
                await Task.Delay(1000);
                await webhookClient.SendMessageAsync("I would like to board the Death Star and head to the cantina.");

                await textChannel.TriggerTypingAsync();
                await Task.Delay(1000);
                await textChannel.SendMessageAsync("In order to board the Death Star and gain access to all our facilities, you'll have to accept our rules.");
            });
        }
    }
}
