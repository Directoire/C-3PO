using C_3PO.Assets;
using C_3PO.Common;
using C_3PO.Data.Context;
using C_3PO.Data.Models;
using Discord;
using Discord.Addons.Hosting;
using Discord.Interactions;
using Discord.Webhook;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace C_3PO.Services
{
    internal class OnboardingHandler : DiscordClientService
    {
        private readonly IServiceProvider _provider;
        private readonly InteractionService _interactionService;
        private readonly AppConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppDbContext _dbContext;

        public OnboardingHandler(
            DiscordSocketClient client,
            ILogger<DiscordClientService> logger,
            IServiceProvider provider,
            InteractionService interactionService,
            AppConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            AppDbContext dbContext)
            : base(client, logger)
        {
            _provider = provider;
            _interactionService = interactionService;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _dbContext = dbContext;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Client.UserJoined += HandleUserJoined;
            Client.ButtonExecuted += HandleButtonExecuted;
            Client.Ready += async () =>
            {
                await HandleUserJoined(Client.Guilds.First()!.GetUser(506954191273721856));
            };
            return Task.CompletedTask;
        }

        private async Task HandleButtonExecuted(SocketMessageComponent component)
        {
            if (((SocketGuildChannel)component.Channel).Guild.Id != _configuration.Guild)
                return;

            var guild = Client.GetGuild(_configuration.Guild);

            await Task.Run(async () =>
            {
                if (!_dbContext.Onboardings
                    .Where(x => x.Id == component.User.Id)
                    .Any())
                {
                    return;
                }

                var user = guild.GetUser(component.User.Id);

                switch(component.Data.CustomId)
                {
                    case "cooperate":
                        await component.DeferAsync();
                        await component.Message.ModifyAsync(x => x.Components = new ComponentBuilder().Build());
                        var userWebhook = await GetOrCreateWebhookAsync(user.Username, user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl(), (ITextChannel)component.Channel);
                        await userWebhook.SendMessageAsync("*Shows identification*");
                        await userWebhook.SendMessageAsync("https://media.giphy.com/media/401C6bNoACPlwbaLCN/giphy.gif");
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        await userWebhook.SendMessageAsync("I would like to board and enter the cantina.");

                        _dbContext.Onboardings
                            .First(x => x.Id == user.Id)
                            .State = OnboardingState.Cooperate;

                        await _dbContext.SaveChangesAsync();
                        await Rules();
                        break;
                    default:
                        throw new NotImplementedException();
                }
            });

            async Task Rules()
            {
                await component.Channel.TriggerTypingAsync();
                await Task.Delay(TimeSpan.FromSeconds(2));
                await component.Channel.SendMessageAsync("In order to head to the cantina, you first need to go through our onboarding procedure.");
                await component.Channel.TriggerTypingAsync();
                await Task.Delay(TimeSpan.FromSeconds(2));
                await component.Channel.SendMessageAsync("First, let's go through the code of conduct.");

                var rules = await guild!.GetTextChannel(_configuration.RulesChannel).GetMessageAsync(_configuration.RulesMessage);
                await component.Channel.SendMessageAsync(rules.Content);
                var rulesComponents = new ComponentBuilder()
                    .WithButton("Yes", "accept", ButtonStyle.Success)
                    .WithButton("No", "reject", ButtonStyle.Danger)
                    .Build();
                await component.Channel.SendMessageAsync("Will you follow these rules?", components: rulesComponents);

                _dbContext.Onboardings
                            .First(x => x.Id == component.User.Id)
                            .State = OnboardingState.Cooperate;

                await _dbContext.SaveChangesAsync();
            }
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

                // Create and gets the webhooks for Darth Vader, Trooper and the user.
                var darthVaderWebhook = await GetOrCreateWebhookAsync("Darth Vader", AppAssets.Avatars.Vader, textChannel);
                var trooperWebhook = await GetOrCreateWebhookAsync("Trooper", AppAssets.Avatars.Trooper, textChannel);
                var userWebhook = await GetOrCreateWebhookAsync(user.Username, user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl(), textChannel);

                await userWebhook.SendMessageAsync(AppAssets.GIFs.ShipInbound);

                await Task.Delay(1000);
                await trooperWebhook.SendMessageAsync("Hello there, unidentified ship. You don’t appear to be in our list of known ships. We are putting you in Docking Bay 327.");
                await Task.Delay(TimeSpan.FromSeconds(1));
                await trooperWebhook.SendMessageAsync("I am dispatching a squad of troopers. Prepare your ship to be boarded for inspection.");
                await Task.Delay(TimeSpan.FromSeconds(5));
                await trooperWebhook.SendMessageAsync("https://media.giphy.com/media/3owzVZFPH8ekSCIo5G/giphy.gif");

                var actionComponents = new ComponentBuilder()
                    .WithButton("Cooperate", "cooperate", ButtonStyle.Success)
                    .WithButton("Attack", "attack", ButtonStyle.Danger)
                    .Build();
                await Task.Delay(1000);
                var actionMessage = await textChannel.SendMessageAsync("The troopers have arrived at your ship. What do you do?", components: actionComponents);

                _dbContext.Add(new Onboarding { Id = user.Id, Channel = textChannel.Id, ActionMessage = actionMessage.Id });
                await _dbContext.SaveChangesAsync();
            });
        }

        private async Task<DiscordWebhookClient> GetOrCreateWebhookAsync(string username, string avatarPath, ITextChannel channel)
        {
            using var httpClient = _httpClientFactory.CreateClient();

            Stream avatar;
            if (avatarPath.StartsWith("https://"))
                avatar = await httpClient.GetStreamAsync(avatarPath);
            else
                avatar = new MemoryStream(File.ReadAllBytes(avatarPath));

            var webhook = (await channel.GetWebhooksAsync())?.FirstOrDefault(x => x.Name == username);
            if (webhook != null)
                return new DiscordWebhookClient(webhook);
            else
                return new DiscordWebhookClient(await channel.CreateWebhookAsync(username, avatar));
        }
    }
}
