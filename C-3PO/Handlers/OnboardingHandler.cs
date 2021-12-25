﻿using C_3PO.Assets;
using C_3PO.Data.Context;
using C_3PO.Data.Models;
using Discord;
using Discord.Addons.Hosting;
using Discord.Webhook;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace C_3PO.Handlers
{
    internal class OnboardingHandler : DiscordClientService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppDbContext _dbContext;

        public OnboardingHandler(
            DiscordSocketClient client,
            ILogger<DiscordClientService> logger,
            IHttpClientFactory httpClientFactory,
            AppDbContext dbContext)
            : base(client, logger)
        {
            _httpClientFactory = httpClientFactory;
            _dbContext = dbContext;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Client.UserJoined += HandleUserJoined;
            Client.ButtonExecuted += HandleButtonExecuted;
            return Task.CompletedTask;
        }

        private Task HandleButtonExecuted(SocketMessageComponent component)
        {
            var configuration = _dbContext.Configurations.First();

            // Check if the component is being executed within the configured guild.
            if (((SocketGuildChannel)component.Channel).Guild.Id != configuration.Id)
                throw new InvalidOperationException("This bot can only be in one guild, please remove it from the guilds that it shouldn't be in.");

            var guild = Client.GetGuild(configuration.Id);

            // Put the execution on a separate thread to prevent blocking.
            Task.Run(async () =>
            {
                // Check whether the user that pressed the button is part of an onboarding procedure. If not, return.
                if (!_dbContext.Onboardings
                    .Where(x => x.Id == component.User.Id)
                    .Any())
                {
                    return;
                }

                // Get various values that are used in multiple parts of the switch case, such as the user as a SocketGuildUser.

                var user = guild.GetUser(component.User.Id);
                var userWebhook = await GetOrCreateWebhookAsync(user.Username, user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl(), (ITextChannel)component.Channel);
                var darthVaderWebhook = await GetOrCreateWebhookAsync("Darth Vader", AppAssets.Avatars.Vader, (ITextChannel)component.Channel);
                var trooperWebhook = await GetOrCreateWebhookAsync("Trooper", AppAssets.Avatars.Trooper, (ITextChannel)component.Channel);
                var onboarding = _dbContext.Onboardings.First(x => x.Id == user.Id);
                var ejectedRole = guild.GetRole(configuration.Ejected);
                var civilianRole = guild.GetRole(configuration.Civilian);

                switch (component.Data.CustomId)
                {
                    case "cooperate":
                        await component.DeferAsync();
                        await component.Message.ModifyAsync(x => x.Components = new ComponentBuilder().Build());

                        await userWebhook.SendMessageAsync("*cooperates and shows identification*");
                        await userWebhook.SendMessageAsync(AppAssets.GIFs.Handshake);
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

                        await userWebhook.SendMessageAsync("*starts fighting the troopers*");
                        await userWebhook.SendMessageAsync(AppAssets.GIFs.Fight);
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        await trooperWebhook.SendMessageAsync("*starts fighting the intruder*");
                        await Task.Delay(TimeSpan.FromSeconds(2));

                        // Outcome of the fight, determined by flipism. 0 indicates a success, 1 indicates a fail.
                        int outcome = new Random().Next(0, 2);
                        if (outcome == 0)
                        {
                            await userWebhook.SendMessageAsync("*wins the fight*");
                            await Task.Delay(TimeSpan.FromSeconds(1));

                            await darthVaderWebhook.SendMessageAsync("*takes the blaster from you*");
                            await darthVaderWebhook.SendMessageAsync(AppAssets.GIFs.VaderShowOff);
                            await Task.Delay(TimeSpan.FromSeconds(1));
                            await darthVaderWebhook.SendMessageAsync("Let’s stop the quarrel, shall we? Stranger, your shooting skills seem quite good. You may continue boarding, if you teach my troopers some of your shooting skills.");

                            var offerComponent = new ComponentBuilder()
                                .WithButton("Accept", "accept_offer", ButtonStyle.Success)
                                .WithButton("Reject", "reject_offer", ButtonStyle.Danger)
                                .Build();
                            await component.Channel.SendMessageAsync("Do you accept or reject Darth Vader's offer?", components: offerComponent);

                            onboarding.State = OnboardingState.Offer;
                            await _dbContext.SaveChangesAsync();
                            return;
                        }

                        await userWebhook.SendMessageAsync(AppAssets.GIFs.ThrownInSpace);
                        await userWebhook.SendMessageAsync("*loses the battle and gets ejected into space*");
                        
                        await user.AddRoleAsync(ejectedRole);
                        _dbContext.Remove(onboarding);
                        await _dbContext.SaveChangesAsync();

                        await Task.Delay(TimeSpan.FromSeconds(3));
                        await ((SocketGuildChannel)component.Channel).DeleteAsync();
                        break;
                    case "accept_offer":
                        await component.DeferAsync();
                        await component.Message.ModifyAsync(x => x.Components = new ComponentBuilder().Build());

                        await userWebhook.SendMessageAsync("Sounds like a plan, Darth Vader.");
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
                            await userWebhook.SendMessageAsync("Hah, you should teach your troopers on your own!");
                            await Task.Delay(TimeSpan.FromSeconds(1));
                        }

                        await darthVaderWebhook.SendMessageAsync("Troopers, throw this stranger back into space!");
                        await darthVaderWebhook.SendMessageAsync(AppAssets.GIFs.VaderByeBye);
                        await userWebhook.SendMessageAsync("*gets ejected into space*");

                        await user.AddRoleAsync(ejectedRole);
                        _dbContext.Remove(onboarding);
                        await _dbContext.SaveChangesAsync();

                        await Task.Delay(TimeSpan.FromSeconds(3));
                        await ((SocketGuildChannel)component.Channel).DeleteAsync();
                        break;
                    case var customId when ulong.TryParse(customId, out var parsedId):
                        await component.DeferAsync();

                        // Determine whether the parsedId is a category or notification role.
                        if (_dbContext.Categories.All(x => x.Id != parsedId) &&
                            _dbContext.NotificationRoles.All(x => x.Id != parsedId))
                            return;

                        // If the parsedId is a category, assign the role of the category to the user.
                        if (_dbContext.Categories.Any(x => x.Id == parsedId))
                        {
                            var categoryRole = guild.GetRole(_dbContext.Categories.First(x => x.Id == parsedId).Role);
                            await user.AddRoleAsync(categoryRole);
                            await userWebhook.SendMessageAsync($"*ticks {categoryRole.Name}*");

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

                        // If the parsedId is a notificationRole, assign the notification role to the user.
                        var role = guild.GetRole(parsedId);
                        await user.AddRoleAsync(role);
                        await userWebhook.SendMessageAsync($"*ticks {role.Name}*");

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

                        var onboardingRole = guild.GetRole(configuration.Onboarding);
                        await user.RemoveRoleAsync(onboardingRole);
                        await user.AddRoleAsync(civilianRole);

                        var welcomeChannel = guild.GetTextChannel(configuration.Hangar);
                        await welcomeChannel.SendMessageAsync($"A new ship has just landed! Welcome {user.Mention}!");

                        string? lastGif = (await welcomeChannel.GetMessagesAsync().FlattenAsync()).Last().Content;

                        string[] gifs = {
                            "https://media.giphy.com/media/xT1R9HVTnpLVbAZ0OI/giphy.gif",
                            "https://media.giphy.com/media/3owzWgWD37dvdlWxqg/giphy.gif",
                            "https://media.giphy.com/media/3o7btXBwXqJ9iDj6U0/giphy.gif",
                            "https://media.giphy.com/media/3o7btTuxoZaEvg5oUo/giphy.gif",
                            "https://media.giphy.com/media/3og0ITP200ZuaAzz2g/giphy.gif",
                            "https://media.giphy.com/media/ddnOwjgipKygIGcUUm/giphy.gif",
                            "https://media.giphy.com/media/bFl5q5fN7AhC9Sz33U/giphy.gif"
                        };

                        var nextGif = gifs[new Random().Next(0, gifs.Length)];
                        if (!string.IsNullOrEmpty(lastGif))
                            nextGif = gifs.Where(x => x != lastGif).ElementAt(new Random().Next(0, gifs.Length));

                        await welcomeChannel.SendMessageAsync(gifs.Where(x => x != lastGif).ElementAt(new Random().Next(0, gifs.Length)));

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
                await component.Channel.SendMessageAsync("In order to head to the cantina, you'll need to go through our onboarding procedure.");
                await component.Channel.TriggerTypingAsync();
                await Task.Delay(TimeSpan.FromSeconds(2));
                await component.Channel.SendMessageAsync("First, let's go through the rules of Efehan's Hangout.");

                var rules = (await guild!.GetTextChannel(configuration.Rules).GetMessagesAsync().FlattenAsync()).First();

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
            // Run the execution on a separate task to prevent blocking.
            await Task.Run(async () =>
            {
                var configuration = _dbContext.Configurations.First();

                // Check if the user is banned and cancel the onboarding process if true.
                if (_dbContext.Infractions
                    .Where(x => x.Active && x.Type == InfractionType.Ban && x.User == user.Id)
                    .Any())
                {
                    var ban = _dbContext.Infractions.First(x => x.Active && x.Type == InfractionType.Ban && x.User == user.Id);

                    // Check if the ban has expired. If true, set the infraction to inactive and continue. If false, cancel the onboarding process.
                    if (ban.ExpiresOn != default(DateTime) && ban.ExpiresOn <= DateTime.Now)
                    {
                        ban.Active = false;
                        await _dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        var ejected = Client.GetGuild(configuration.Id).GetRole(configuration.Ejected);
                        await user.AddRoleAsync(ejected);
                        return;
                    }
                }

                // Check if the event was triggered within the configured guild.
                if (user.Guild.Id != configuration.Id)
                    throw new InvalidOperationException("This bot can only be in one guild, please remove it from the guilds that it shouldn't be in.");

                // Assign the user the onboarding role, preventing them from speaking in categories until their onboarding procedure is finished.
                var onboarding = user.Guild.GetRole(configuration.Onboarding);
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
                    t.CategoryId = configuration.OuterRim;
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