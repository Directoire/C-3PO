using C_3PO.Assets;
using C_3PO.Common;
using C_3PO.Data.Context;
using C_3PO.Data.Models;
using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;

namespace C_3PO.Services
{
    public class OnboardingService
    {
        private readonly DiscordSocketClient _client;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly LogsService _logsService;
        private readonly AppConfiguration _configuration;

        public static IDictionary<ulong, CancellationTokenSource> StartingProcedures = new Dictionary<ulong, CancellationTokenSource>();

        public OnboardingService(
            DiscordSocketClient client,
            IHttpClientFactory httpClientFactory,
            IServiceProvider serviceProvider,
            LogsService logsService, AppConfiguration configuration)
        {
            _client = client;
            _httpClientFactory = httpClientFactory;
            _serviceProvider = serviceProvider;
            _logsService = logsService;
            _configuration = configuration;
        }

        public async Task StartOnboarding(SocketGuildUser user, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Check if the event was triggered within the configured guild.
            if (user.Guild.Id != _configuration.Guild)
                throw new InvalidOperationException("This bot can only be in one guild, please remove it from the guilds that it shouldn't be in.");

            // Assign the user the onboarding role, preventing them from speaking in categories until their onboarding procedure is finished.
            var onboarding = user.Guild.GetRole(_configuration.Roles.Onboarding);
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
                t.CategoryId = _configuration.Categories.OuterRim;
                t.PermissionOverwrites = permissionOverwrites;
            });

            try
            {
                // Create and gets the webhooks for Darth Vader, Trooper and the user.
                var darthVaderWebhook = await textChannel.GetOrCreateWebhookAsync("Darth Vader", AppAssets.Avatars.Vader, _httpClientFactory.CreateClient());
                var trooperWebhook = await textChannel.GetOrCreateWebhookAsync("Trooper", AppAssets.Avatars.Trooper, _httpClientFactory.CreateClient());
                var userWebhook = await textChannel.GetOrCreateWebhookAsync(user.Username, user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl(), _httpClientFactory.CreateClient());

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

                // Remove the procedure from the list of starting procedures, as it can be assumed that the record will be stored and hence the "starting" has finished.
                StartingProcedures.Remove(user.Id);

                // Perform clean-up if cancellation was requested.
                if (cancellationToken.IsCancellationRequested)
                {
                    await textChannel.DeleteAsync();
                    return;
                }

                var entity = dbContext.Add(new Onboarding { Id = user.Id, Channel = textChannel.Id, ActionMessage = actionMessage.Id });
                await dbContext.SaveChangesAsync();
                await _logsService.Log($"An onboarding procedure was successfully started for {user.Mention}.");
            }
            catch (TimeoutException ex)
            {
                await textChannel.SendMessageAsync($"Beep-boop... I'm having issues starting the onboarding process. " +
                $"This may be because you're joining and leaving too quickly or because many people are currently joining Efehan's hangout. " +
                $"I will try to run the onboarding process again <t:{(DateTimeOffset.Now + TimeSpan.FromMinutes(5)).ToUnixTimeSeconds()}:R>.");

                Log.Warning(ex.ToString());
                await Task.Delay(TimeSpan.FromMinutes(5));

                if (textChannel != null)
                    await textChannel.DeleteAsync();

                await StartOnboarding(user, cancellationToken);
                return;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                if (textChannel != null)
                {
                    await textChannel.SendMessageAsync("Beep-boop... I'm having issues starting the onboarding process.");
                    await textChannel.SendMessageAsync("<@506954191273721856> your presence is required, my lord.");
                }
                
                return;
            }
        }

        public async Task ProcessButton(AppDbContext dbContext, SocketMessageComponent component)
        {
            var guild = _client.GetGuild(_configuration.Guild);

            // Get various values that are used in multiple parts of the switch case, such as the user as a SocketGuildUser.
            var user = guild.GetUser(component.User.Id);
            var userWebhook = await component.Channel.GetOrCreateWebhookAsync(user.Username, user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl(), _httpClientFactory.CreateClient());
            var darthVaderWebhook = await component.Channel.GetOrCreateWebhookAsync("Darth Vader", AppAssets.Avatars.Vader, _httpClientFactory.CreateClient());
            var trooperWebhook = await component.Channel.GetOrCreateWebhookAsync("Trooper", AppAssets.Avatars.Trooper, _httpClientFactory.CreateClient());
            var onboarding = dbContext.Onboardings.First(x => x.Id == user.Id);
            var ejectedRole = guild.GetRole(_configuration.Roles.Ejected);
            var civilianRole = guild.GetRole(_configuration.Roles.Civilian);

            switch (component.Data.CustomId)
            {
                case "cooperate":
                    await component.DeferAsync();
                    await component.Message.ModifyAsync(x => x.Components = new ComponentBuilder().Build());

                    await userWebhook.SendMessageAsync("*cooperates and shows identification*");
                    await userWebhook.SendMessageAsync(AppAssets.GIFs.Handshake);
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    await userWebhook.SendMessageAsync("I would like to board and enter the cantina.");

                    dbContext.Onboardings
                        .First(x => x.Id == user.Id)
                        .State = OnboardingState.Cooperate;

                    await dbContext.SaveChangesAsync();
                    await SendRules();
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
                        await dbContext.SaveChangesAsync();
                        return;
                    }

                    await userWebhook.SendMessageAsync(AppAssets.GIFs.ThrownInSpace);
                    await userWebhook.SendMessageAsync("*loses the battle and gets ejected into space*");

                    await user.AddRoleAsync(ejectedRole);
                    dbContext.Remove(onboarding);
                    await dbContext.SaveChangesAsync();
                    await _logsService.Log($"{user.Mention} was thrown into space as they failed the fight.");

                    await Task.Delay(TimeSpan.FromSeconds(3));
                    await ((SocketGuildChannel)component.Channel).DeleteAsync();
                    break;
                case "accept_offer":
                    await component.DeferAsync();
                    await component.Message.ModifyAsync(x => x.Components = new ComponentBuilder().Build());

                    await userWebhook.SendMessageAsync("Sounds like a plan, Darth Vader.");
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    await SendRules();
                    break;
                case "accept_rules":
                    await component.DeferAsync();
                    await component.Message.ModifyAsync(x => x.Components = new ComponentBuilder().Build());

                    await userWebhook.SendMessageAsync("Yes, I will follow the rules.");

                    // TODO: implement "all" button
                    var categoriesComponent = new ComponentBuilder()
                        .WithButton("Continue", "continue", ButtonStyle.Success);

                    foreach (var category in dbContext.Categories)
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

                    await dbContext.SaveChangesAsync();
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
                    dbContext.Remove(onboarding);
                    await dbContext.SaveChangesAsync();

                    await _logsService.Log($"{user.Mention} was thrown into space because they rejected the {(component.Data.CustomId == "reject_offer" ? "offer" : "rules")}.");

                    await Task.Delay(TimeSpan.FromSeconds(3));
                    await ((SocketGuildChannel)component.Channel).DeleteAsync();
                    break;
                case var customId when ulong.TryParse(customId, out var parsedId):
                    await component.DeferAsync();

                    // Determine whether the parsedId is a category or notification role.
                    if (dbContext.Categories.All(x => x.Id != parsedId) &&
                        dbContext.NotificationRoles.All(x => x.Id != parsedId))
                        return;

                    // TODO: check if all categories/notification roles have been clicked and automatically continue (without needing to click finish)

                    // If the parsedId is a category, assign the role of the category to the user.
                    if (dbContext.Categories.Any(x => x.Id == parsedId))
                    {
                        var categoryRole = guild.GetRole(dbContext.Categories.First(x => x.Id == parsedId).Role);
                        await user.AddRoleAsync(categoryRole);
                        await userWebhook.SendMessageAsync($"*ticks {categoryRole.Name}*");

                        var leftCategoriesComponent = new ComponentBuilder()
                            .WithButton("Continue", "continue", ButtonStyle.Success);

                        foreach (var category in dbContext.Categories)
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

                    foreach (var notificationRole in dbContext.NotificationRoles.Include(x => x.Category))
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

                    // TODO: implement "all" button
                    var notificationsComponent = new ComponentBuilder()
                        .WithButton("Finish", "finish", ButtonStyle.Success);

                    foreach (var notificationRole in dbContext.NotificationRoles.Include(x => x.Category))
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
                    await dbContext.SaveChangesAsync();
                    break;
                case "finish":
                    await component.DeferAsync();
                    await component.Message.ModifyAsync(x => x.Components = new ComponentBuilder().Build());
                    await component.Channel.TriggerTypingAsync();
                    await Task.Delay(TimeSpan.FromSeconds(2));
                    await component.Channel.SendMessageAsync("You’ve finished the onboarding procedure. Welcome to Efehan’s Hangout! You will now be moved to the cantina...");
                    await Task.Delay(TimeSpan.FromSeconds(2));

                    var onboardingRole = guild.GetRole(_configuration.Roles.Onboarding);
                    await user.RemoveRoleAsync(onboardingRole);
                    await user.AddRoleAsync(civilianRole);

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

                    var welcomeChannel = guild.GetTextChannel(_configuration.Channels.Hangar);
                    await welcomeChannel.SendMessageAsync($"A new ship has just landed! Welcome {user.Mention}!");
                    await welcomeChannel.SendMessageAsync(nextGif);

                    dbContext.Remove(onboarding);
                    await dbContext.SaveChangesAsync();

                    await ((SocketGuildChannel)component.Channel).DeleteAsync();
                    await _logsService.Log($"{user.Mention} successfully finished the onboarding procedure.");
                    break;
                default:
                    break;
            }

            async Task SendRules()
            {
                await component.Channel.TriggerTypingAsync();
                await Task.Delay(TimeSpan.FromSeconds(2));
                await component.Channel.SendMessageAsync("In order to head to the cantina, you'll need to go through our onboarding procedure.");
                await component.Channel.TriggerTypingAsync();
                await Task.Delay(TimeSpan.FromSeconds(2));
                await component.Channel.SendMessageAsync("First, let's go through the rules of Efehan's Hangout.");

                var rules = (await guild!.GetTextChannel(_configuration.Channels.Rules).GetMessagesAsync().FlattenAsync()).First();

                var rulesEmbed = new EmbedBuilder()
                    .WithDescription(rules.Content)
                    .WithColor(Colours.Primary)
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

                dbContext.Onboardings
                    .First(x => x.Id == component.User.Id)
                    .State = OnboardingState.Cooperate;

                await dbContext.SaveChangesAsync();
            }
        }
    }
}
