using C_3PO.Assets;
using C_3PO.Common;
using C_3PO.Data.Context;
using C_3PO.Data.Models;
using Discord;
using Discord.Addons.Hosting;
using Discord.Interactions;
using Discord.Webhook;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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
            //Client.Ready += async () =>
            //{
            //    if (_dbContext.Onboardings.Any(x => x.Id == 506954191273721856))
            //    {
            //        _dbContext.Remove(_dbContext.Onboardings
            //            .First(x => x.Id == 506954191273721856));

            //        await _dbContext.SaveChangesAsync();
            //    }

            //    var category = Client.Guilds.First().GetCategoryChannel(_configuration.OnboardingCategory);
            //    foreach (var channel in category.Channels.Where(x => x.Id != 922105571484782612))
            //    {
            //        await channel.DeleteAsync();
            //    }

            //    await HandleUserJoined(Client.Guilds.First()!.GetUser(506954191273721856));
            //};
            return Task.CompletedTask;
        }

        private Task HandleButtonExecuted(SocketMessageComponent component)
        {
            if (((SocketGuildChannel)component.Channel).Guild.Id != _configuration.Guild)
                return Task.CompletedTask;

            var guild = Client.GetGuild(_configuration.Guild);

            Task.Run(async () =>
            {
                if (!_dbContext.Onboardings
                    .Where(x => x.Id == component.User.Id)
                    .Any())
                {
                    return;
                }

                var user = guild.GetUser(component.User.Id);
                var userWebhook = await GetOrCreateWebhookAsync(user.Username, user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl(), (ITextChannel)component.Channel);
                var darthVaderWebhook = await GetOrCreateWebhookAsync("Darth Vader", AppAssets.Avatars.Vader, (ITextChannel)component.Channel);
                var trooperWebhook = await GetOrCreateWebhookAsync("Trooper", AppAssets.Avatars.Trooper, (ITextChannel)component.Channel);
                var onboarding = _dbContext.Onboardings.First(x => x.Id == user.Id);
                var ejectedRole = guild.GetRole(_configuration.EjectedRole);
                var ejectedChannel = guild.GetTextChannel(_configuration.EjectedChannel);
                var civilianRole = guild.GetRole(_configuration.CivilianRole);

                switch (component.Data.CustomId)
                {
                    case "cooperate":
                        await component.DeferAsync();
                        await component.Message.ModifyAsync(x => x.Components = new ComponentBuilder().Build());
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
                    case "attack":
                        await component.DeferAsync();
                        await component.Message.ModifyAsync(x => x.Components = new ComponentBuilder().Build());

                        await userWebhook.SendMessageAsync("https://media.giphy.com/media/4WFgXmhbqWCowRTmdN/giphy.gif");
                        await userWebhook.SendMessageAsync("*Starts fighting the troopers*");

                        await Task.Delay(TimeSpan.FromSeconds(1));
                        await trooperWebhook.SendMessageAsync("*Starts shooting at the intruder*");
                        await Task.Delay(TimeSpan.FromSeconds(2));

                        // Outcome of the fight, determined by flipism. 0 indicates a success, 1 indicates a fail.
                        int outcome = new Random().Next(0, 2);
                        if (outcome == 0)
                        {
                            await darthVaderWebhook.SendMessageAsync("*Takes the blaster from you*");
                            await darthVaderWebhook.SendMessageAsync("https://media.giphy.com/media/xTiIzsz8RsfugNsg6s/giphy.gif");
                            
                            await Task.Delay(TimeSpan.FromSeconds(1));
                            await darthVaderWebhook.SendMessageAsync("Let’s stop the quarrel, shall we? Stranger, your shooting skills seem quite good. Care to show my troopers how they can do that in the cantina?");

                            var offerComponent = new ComponentBuilder()
                                .WithButton("Accept", "accept_offer", ButtonStyle.Success)
                                .WithButton("Reject", "reject_offer", ButtonStyle.Danger)
                                .Build();

                            await component.Channel.SendMessageAsync("Do you accept or reject Darth Vader's offer?", components: offerComponent);

                            onboarding.State = OnboardingState.Offer;
                            await _dbContext.SaveChangesAsync();
                            return;
                        }

                        await userWebhook.SendMessageAsync("https://media.giphy.com/media/l1KsqPhtSrgrJ9dO8/giphy.gif");
                        await userWebhook.SendMessageAsync("*Loses the battle and gets ejected into space*");
                        
                        await user.AddRoleAsync(ejectedRole);
                        _dbContext.Remove(onboarding);
                        await _dbContext.SaveChangesAsync();

                        await Task.Delay(TimeSpan.FromSeconds(3));
                        await ((SocketGuildChannel)component.Channel).DeleteAsync();
                        break;
                    case "accept_offer":
                        await component.DeferAsync();
                        await component.Message.ModifyAsync(x => x.Components = new ComponentBuilder().Build());

                        await userWebhook.SendMessageAsync("I accept the offer.");
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        await Rules();
                        break;
                    case "accept_rules":
                        await component.DeferAsync();
                        await component.Message.ModifyAsync(x => x.Components = new ComponentBuilder().Build());

                        await userWebhook.SendMessageAsync("Yes, I will follow the rules.");

                        var categoriesComponent = new ComponentBuilder()
                            .WithButton("Continue", "continue", ButtonStyle.Success);

                        foreach (var category in _dbContext.Categories)
                        {
                            var name = guild.GetCategoryChannel(category.Id).Name;
                            categoriesComponent.WithButton($"Join {name}", category.Id.ToString(), ButtonStyle.Secondary);
                        }

                        await component.Channel.TriggerTypingAsync();
                        await Task.Delay(TimeSpan.FromSeconds(2));

                        var categoriesMessage = await component.Channel.SendMessageAsync(
                            "Great! You can join various categories giving you access to related channels. For instance, you can join programming to request help or showcase your projects.",
                            components: categoriesComponent.Build());

                        onboarding.CategoriesMessage = categoriesMessage.Id;
                        onboarding.State = OnboardingState.Categories;

                        await _dbContext.SaveChangesAsync();
                        break;
                    case "reject_rules":
                    case "reject_offer":
                        await component.DeferAsync();
                        await component.Message.ModifyAsync(x => x.Components = new ComponentBuilder().Build());
                        if (component.Data.CustomId == "reject_offer")
                        {
                            await userWebhook.SendMessageAsync("I reject the offer!");
                            await Task.Delay(TimeSpan.FromSeconds(1));
                        }

                        await darthVaderWebhook.SendMessageAsync("https://media.giphy.com/media/xTiIzKvMhb6ML9gEtG/giphy.gif");
                        await userWebhook.SendMessageAsync("*Gets ejected into space*");

                        await user.AddRoleAsync(ejectedRole);
                        _dbContext.Remove(onboarding);
                        await _dbContext.SaveChangesAsync();

                        await Task.Delay(TimeSpan.FromSeconds(3));
                        await ((SocketGuildChannel)component.Channel).DeleteAsync();
                        break;
                    case var customId when ulong.TryParse(customId, out var parsedId):
                        await component.DeferAsync();

                        if (_dbContext.Categories.All(x => x.Id != parsedId) &&
                            _dbContext.NotificationRoles.All(x => x.Id != parsedId))
                            return;

                        if (_dbContext.Categories.Any(x => x.Id == parsedId))
                        {
                            var categoryRole = guild.GetRole(_dbContext.Categories.First(x => x.Id == parsedId).Role);
                            await user.AddRoleAsync(categoryRole);
                            await userWebhook.SendMessageAsync($"*Ticks {categoryRole.Name}*");

                            var leftCategoriesComponent = new ComponentBuilder()
                                .WithButton("Continue", "continue", ButtonStyle.Success);

                            foreach (var category in _dbContext.Categories)
                            {
                                if (user.Roles.Any(x => x.Id == category.Role) || category.Id == parsedId)
                                    continue;

                                var name = guild.GetCategoryChannel(category.Id).Name;
                                leftCategoriesComponent.WithButton($"Join {name}", category.Id.ToString(), ButtonStyle.Secondary);
                            }

                            await component.Message.ModifyAsync(x => x.Components = leftCategoriesComponent.Build());
                            return;
                        }

                        var role = guild.GetRole(parsedId);
                        await user.AddRoleAsync(role);
                        await userWebhook.SendMessageAsync($"*Ticks {role.Name}*");

                        var leftNotificationsComponent = new ComponentBuilder()
                            .WithButton("Finish", "finish", ButtonStyle.Success);

                        foreach (var notificationRole in _dbContext.NotificationRoles)
                        {
                            if (user.Roles.Any(x => x.Id == notificationRole.Id) || notificationRole.Id == parsedId)
                                continue;

                            if (notificationRole.Category != null && user.Roles.All(x => x.Id != notificationRole.Category?.Role))
                            {
                                continue;
                            }

                            leftNotificationsComponent.WithButton(
                                $"Join {guild.GetRole(notificationRole.Id).Name}",
                                notificationRole.Id.ToString(),
                                ButtonStyle.Secondary);
                        }

                        await component.Message.ModifyAsync(x => x.Components = leftNotificationsComponent.Build());
                        break;
                    case "continue":
                        await component.DeferAsync();
                        await component.Message.ModifyAsync(x => x.Components = new ComponentBuilder().Build());

                        var notificationsComponent = new ComponentBuilder()
                            .WithButton("Finish", "finish", ButtonStyle.Success);

                        foreach (var notificationRole in _dbContext.NotificationRoles)
                        {
                            if (notificationRole.Category != null && user.Roles.All(x => x.Id != notificationRole.Category?.Role))
                            {
                                continue;
                            }

                            notificationsComponent.WithButton(
                                $"Join {guild.GetRole(notificationRole.Id).Name}",
                                notificationRole.Id.ToString(),
                                ButtonStyle.Secondary);
                        }

                        await component.Channel.TriggerTypingAsync();
                        await Task.Delay(TimeSpan.FromSeconds(2));

                        await component.Channel.SendMessageAsync(
                            "Finally, there are also some notifications you can subscribe to. These are server-wide and per category.",
                            components: notificationsComponent.Build());

                        onboarding.State = OnboardingState.Notifications;
                        await _dbContext.SaveChangesAsync();
                        break;
                    case "finish":
                        await component.DeferAsync();
                        await component.Message.ModifyAsync(x => x.Components = new ComponentBuilder().Build());
                        await component.Channel.TriggerTypingAsync();
                        await Task.Delay(TimeSpan.FromSeconds(2));
                        await component.Channel.SendMessageAsync("You’ve finished the onboarding procedure. Welcome to Efehan’s Hangout! You will now be moved to the cantina...");
                        await Task.Delay(TimeSpan.FromSeconds(2));

                        var onboardingRole = guild.GetRole(_configuration.OnboardingRole);
                        await user.RemoveRoleAsync(onboardingRole);
                        await user.AddRoleAsync(civilianRole);

                        var welcomeChannel = guild.GetTextChannel(_configuration.WelcomeChannel);
                        await welcomeChannel.SendMessageAsync($"A new ship has just landed! Welcome {user.Mention}!");

                        string[] gifs = {
                            "https://media.giphy.com/media/xT1R9HVTnpLVbAZ0OI/giphy.gif",
                            "https://media.giphy.com/media/3owzWgWD37dvdlWxqg/giphy.gif",
                            "https://media.giphy.com/media/3o7btXBwXqJ9iDj6U0/giphy.gif",
                            "https://media.giphy.com/media/3o7btTuxoZaEvg5oUo/giphy.gif",
                            "https://media.giphy.com/media/3og0ITP200ZuaAzz2g/giphy.gif",
                            "https://media.giphy.com/media/ddnOwjgipKygIGcUUm/giphy.gif",
                            "https://media.giphy.com/media/bFl5q5fN7AhC9Sz33U/giphy.gif"
                        };

                        await welcomeChannel.SendMessageAsync(gifs[new Random().Next(0, gifs.Length)]);

                        _dbContext.Remove(onboarding);
                        await _dbContext.SaveChangesAsync();

                        await ((SocketGuildChannel)component.Channel).DeleteAsync();
                        break;
                    default:
                        break;
                }
            });
            return Task.CompletedTask;

            async Task Rules()
            {
                await component.Channel.TriggerTypingAsync();
                await Task.Delay(TimeSpan.FromSeconds(2));
                await component.Channel.SendMessageAsync("In order to head to the cantina, you first need to go through our onboarding procedure.");
                await component.Channel.TriggerTypingAsync();
                await Task.Delay(TimeSpan.FromSeconds(2));
                await component.Channel.SendMessageAsync("First, let's go through the code of conduct.");

                var rules = await guild!.GetTextChannel(_configuration.RulesChannel).GetMessageAsync(_configuration.RulesMessage);

                var rulesEmbed = new EmbedBuilder()
                    .WithDescription(rules.Content)
                    .Build();

                await component.Channel.TriggerTypingAsync();
                await Task.Delay(TimeSpan.FromSeconds(3));
                await component.Channel.SendMessageAsync(embed: rulesEmbed);
                var rulesComponents = new ComponentBuilder()
                    .WithButton("Yes", "accept_rules", ButtonStyle.Success)
                    .WithButton("No", "reject_rules", ButtonStyle.Danger)
                    .Build();

                await component.Channel.TriggerTypingAsync();
                await Task.Delay(TimeSpan.FromSeconds(1));
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

                // Assign the user the onboarding role, preventing them from speaking in categories until their onboarding procedure is finished.
                var onboarding = user.Guild.GetRole(_configuration.OnboardingRole);
                await user.AddRoleAsync(onboarding);

                // Permission overwrites to make the channel only visible for the involved user.
                var permissionOverwrites = new List<Overwrite>()
                {
                    new Overwrite(user.Guild.EveryoneRole.Id, PermissionTarget.Role, new OverwritePermissions(viewChannel: PermValue.Deny)),
                    new Overwrite(user.Id, PermissionTarget.User, new OverwritePermissions(viewChannel: PermValue.Allow, sendMessages: PermValue.Deny))
                };

                // Create the onboarding channel under the onboarding category and assign the permission overwrites.
                var textChannel = await user.Guild.CreateTextChannelAsync(user.Username + "-onboarding", t =>
                {
                    t.CategoryId = _configuration.OnboardingCategory;
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
