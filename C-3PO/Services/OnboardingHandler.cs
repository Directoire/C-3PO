using C_3PO.Assets;
using C_3PO.Common;
using Discord;
using Discord.Addons.Hosting;
using Discord.Interactions;
using Discord.Webhook;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace C_3PO.Services
{
    internal class OnboardingHandler : DiscordClientService
    {
        private readonly IServiceProvider _provider;
        private readonly InteractionService _interactionService;
        private readonly AppConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public OnboardingHandler(
            DiscordSocketClient client,
            ILogger<DiscordClientService> logger,
            IServiceProvider provider,
            InteractionService interactionService,
            AppConfiguration configuration,
            IHttpClientFactory httpClientFactory)
            : base(client, logger)
        {
            _provider = provider;
            _interactionService = interactionService;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Client.UserJoined += HandleUserJoined;
            Client.Ready += async () =>
            {
                await HandleUserJoined(Client.Guilds.First()!.GetUser(506954191273721856));
            };
            return Task.CompletedTask;
        }

        private async Task HandleUserJoined(SocketGuildUser user)
        {
            await Task.Run(async () =>
            {
                if (user.Guild.Id != _configuration.Guild)
                    return;

                // Permission overwrites to make the channel only visible for the involved user.
                var permissionOverwrites = new List<Overwrite>()
                {
                    new Overwrite(user.Guild.EveryoneRole.Id, PermissionTarget.Role, new OverwritePermissions(viewChannel: PermValue.Deny)),
                    new Overwrite(user.Id, PermissionTarget.User, new OverwritePermissions(viewChannel: PermValue.Allow, sendMessages: PermValue.Deny))
                };

                // Create the onboarding channel under the onboarding category and assign the permission overwrites.
                var textChannel = await user.Guild.CreateTextChannelAsync(user.Username + "-onboarding", t =>
                {
                    t.CategoryId = _configuration.Categories.Onboarding;
                    t.PermissionOverwrites = permissionOverwrites;
                });

                // Get the avatars of Darth Vader, Trooper and the user as streams.
                using var httpClient = _httpClientFactory.CreateClient();
                var darthVaderAvatar = new MemoryStream(File.ReadAllBytes(AppAssets.Avatars.Vader));
                var trooperAvatar = new MemoryStream(File.ReadAllBytes(AppAssets.Avatars.Trooper));
                var userAvatar = await httpClient.GetStreamAsync(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl());

                // Create the webhooks for Darth Vader, Trooper and the user.
                var darthVaderWebhook = new DiscordWebhookClient(await textChannel.CreateWebhookAsync("Darth Vader", darthVaderAvatar));
                var trooperWebhook = new DiscordWebhookClient(await textChannel.CreateWebhookAsync("Trooper", trooperAvatar));
                var userWebhook = new DiscordWebhookClient(await textChannel.CreateWebhookAsync(user.Username, userAvatar));

                await userWebhook.SendMessageAsync(AppAssets.GIFs.ShipInbound);

                await Task.Delay(1000);
                await trooperWebhook.SendMessageAsync("Hello there, unidentified ship. You don’t appear to be in our list of known ships. We are putting you in Docking Bay 327.");
                await Task.Delay(TimeSpan.FromSeconds(5));
                await trooperWebhook.SendMessageAsync("I am dispatching a squad of troopers. Prepare your ship to be boarded for inspection.");
                await Task.Delay(TimeSpan.FromSeconds(5));
                await trooperWebhook.SendMessageAsync("https://media.giphy.com/media/3owzVZFPH8ekSCIo5G/giphy.gif");

                var actionComponents = new ComponentBuilder()
                    .WithButton("Cooperate", "cooperate", ButtonStyle.Success)
                    .WithButton("Attack", "attack", ButtonStyle.Danger)
                    .Build();
                await Task.Delay(1000);
                await textChannel.SendMessageAsync("The troopers have arrived at your ship. What do you do?", components: actionComponents);
            });
        }
    }
}
