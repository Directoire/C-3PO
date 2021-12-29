using C_3PO.Assets;
using C_3PO.Data.Context;
using C_3PO.Data.Models;
using C_3PO.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_3PO.Modules
{
    public class ModerationModule : AppInteractionModuleBase
    {
        private readonly OnboardingService _onboardingService;

        public ModerationModule(
            AppDbContext dbContext, 
            OnboardingService onboardingService,
            ILogger<AppInteractionModuleBase> logger)
            : base(dbContext, logger)
        {
            _onboardingService = onboardingService;
        }

        [RequireUserPermission(GuildPermission.KickMembers)]
        [SlashCommand("warn", "Warn a user with an optional reason.")]
        public async Task Warn(
            [Summary("user", "The user to warn.")] SocketGuildUser user,
            [Summary("reason", "The reason behind the warning.")] string reason)
        {
            await DeferAsync();
            DbContext.Add(new Infraction { User = user.Id, Reason = reason, Moderator = Context.User.Id, Type = InfractionType.Warn });
            await DbContext.SaveChangesAsync();

            try
            {
                await user.SendMessageAsync($"Hello there! You have been warned in Efehan's Hangout by {Context.User.Mention}.\n\n```{reason}```");
            }
            catch
            { }

            await UpdateAsync($"Been warned, {user} has.");
        }

        [RequireUserPermission(GuildPermission.KickMembers)]
        [SlashCommand("warnings", "View all warnings of a specific user.")]
        public async Task Warnings([Summary("user", "The user to check.")] SocketGuildUser user)
        {
            await DeferAsync(ephemeral: true);
            var warnings = DbContext.Infractions
                .Where(x => x.User == user.Id && x.Type == InfractionType.Warn && x.Active)
                .ToList();

            if (warnings.Count == 0)
            {
                await UpdateAsync("No warnings, this user has.");
                return;
            }

            var embedBuilder = new EmbedBuilder()
                .WithColor(Colours.Primary)
                .WithDescription($"**Total:** {warnings.Count}");

            if (warnings.Count > 25)
                embedBuilder.WithFooter("This embed only displays the first 25 warnings");

            foreach (var warning in warnings.Take(25))
            {
                var moderator = Context.Guild.GetUser(warning.Moderator);
                embedBuilder.AddField($"From {moderator?.ToString() ?? "unknown"}", $"ID: {warning.Id}\n<t:{((DateTimeOffset)warning.IssuedOn).ToUnixTimeSeconds()}>\nReason: {warning.Reason}");
            }

            await UpdateAsync(embed: embedBuilder.Build());
        }

        [RequireUserPermission(GuildPermission.KickMembers)]
        [SlashCommand("remove-warning", "Remove a warning by ID.")]
        public async Task RemoveWarning([Summary("id", "The ID of the warning.")] int id)
        {
            await DeferAsync();
            var warning = DbContext.Infractions.FirstOrDefault(x => x.Id == id);
            if (warning == null)
            {
                await UpdateAsync("That warning doesn't exist. Hrmmm.");
                return;
            }

            warning.Active = false;
            await DbContext.SaveChangesAsync();
            await UpdateAsync("Been successfully removed, the warning has.");
        }

        [RequireUserPermission(GuildPermission.KickMembers)]
        [SlashCommand("report", "Get an overview of a user's record.")]
        public async Task Report([Summary("user", "The user to search for.")] SocketGuildUser user)
        {
            await DeferAsync(ephemeral: true);
            var infractions = DbContext.Infractions.Where(x => x.User == user.Id);
            var embedBuilder = new EmbedBuilder()
                .WithColor(Colours.Primary);

            if (infractions.Count() == 0)
            {
                embedBuilder.WithDescription("No records, this user has. Impressive.");
                await UpdateAsync(embed: embedBuilder.Build());
                return;
            }

            embedBuilder.WithDescription(
                $"**Warnings:** {infractions.Count(x => x.Type == InfractionType.Warn && x.Active)} {(infractions.Any(x => x.Type == InfractionType.Warn && !x.Active) ? $"({infractions.Count(x => x.Type == InfractionType.Warn && !x.Active)} inactive)" : "")}\n" +
                $"**Kicks:** {infractions.Count(x => x.Type == InfractionType.Kick)}\n" +
                $"**Bans:** {infractions.Count(x => x.Type == InfractionType.Ban && x.Active)} {(infractions.Any(x => x.Type == InfractionType.Ban && !x.Active) ? $"({infractions.Count(x => x.Type == InfractionType.Ban && !x.Active)} inactive)" : "")}");

            await UpdateAsync(embed: embedBuilder.Build());
        }

        [RequireUserPermission(GuildPermission.ManageMessages)]
        [SlashCommand("purge", "Purge a provided amount of messages from the current channel.")]
        public async Task Purge([Summary("amount", "The amount of messages to be purged.")] int amount)
        {
            await DeferAsync(ephemeral: true);
            var messages = await Context.Channel.GetMessagesAsync(amount).FlattenAsync();
            await ((ITextChannel)Context.Channel).DeleteMessagesAsync(messages);
            await UpdateAsync($"Successfully deleted, {messages.Count()} messages were. Yes, hrrmmm.");
        }

        [RequireUserPermission(GuildPermission.KickMembers)]
        [SlashCommand("kick", "Kick a user and send them back into space.")]
        public async Task Kick(
            [Summary("user", "The user to kick.")] SocketGuildUser user,
            [Summary("reason", "The reason behind the kick.")] string? reason = null)
        {
            await DeferAsync();
            await user.RemoveRolesAsync(user.Roles.Where(x => !x.IsEveryone));
            var ejected = Context.Guild.GetRole(DbContext.Configurations.First().Ejected);
            await user.AddRoleAsync(ejected);

            DbContext.Add(new Infraction { User = user.Id, Reason = reason, Moderator = Context.User.Id, Type = InfractionType.Kick });
            await DbContext.SaveChangesAsync();

            try
            {
                await user.SendMessageAsync($"Hello there! You have been kicked from Efehan's Hangout by {Context.User.Mention}. You're now floating in space...{(string.IsNullOrEmpty(reason) ? "" : $"\n\n```{reason}```")}");
            }
            catch
            { }

            await UpdateAsync($"Been thrown off the ship, {user} has.");
        }

        [RequireUserPermission(GuildPermission.BanMembers)]
        [SlashCommand("ban", "Ban a user and send them back into space permanently.")]
        public async Task Ban(
            [Summary("user", "The user to ban.")] SocketGuildUser user,
            [Summary("reason", "The reason behind the ban.")] string? reason = null,
            [Summary("expiresOn", "The date and time when the ban expires.")] string? expiresOnString = null)
        {
            await DeferAsync();

            if (DbContext.Infractions.Any(x => x.User == user.Id && x.Type == InfractionType.Ban && x.Active))
            {
                await UpdateAsync("Already been banned, that user has.");
                return;
            }

            var expiresOn = default(DateTime);
            if (!string.IsNullOrEmpty(expiresOnString))
            {
                if (!DateTime.TryParse(expiresOnString, out expiresOn))
                {
                    await UpdateAsync("Please provide a date that is valid. For instance dd-mm-yyyy.");
                    return;
                }

                if (expiresOn < DateTime.Now)
                {
                    await UpdateAsync("Please provide a date that is in the future.");
                    return;
                }
            }

            await user.RemoveRolesAsync(user.Roles.Where(x => !x.IsEveryone));
            var ejected = Context.Guild.GetRole(DbContext.Configurations.First().Ejected);
            await user.AddRoleAsync(ejected);

            DbContext.Add(new Infraction { User = user.Id, Reason = reason, Moderator = Context.User.Id, Type = InfractionType.Ban, ExpiresOn = expiresOn });
            await DbContext.SaveChangesAsync();

            try
            {
                await user.SendMessageAsync($"Hello there! You have been banned from Efehan's Hangout by {Context.User.Mention}. You're now floating in space...{(string.IsNullOrEmpty(reason) ? "" : $"\n\n```{reason}```")}");
            }
            catch
            { }

            await UpdateAsync($"Been banned from Efehan's Hangout, {user.Mention} has.");
        }

        [RequireUserPermission(GuildPermission.BanMembers)]
        [SlashCommand("unban", "Unban a user, allowing them to board again.")]
        public async Task Unban([Summary("user", "The user to unban.")] SocketGuildUser user)
        {
            await DeferAsync();

            var ban = DbContext.Infractions.FirstOrDefault(x => x.User == user.Id && x.Type == InfractionType.Ban && x.Active);
            if (ban == null)
            {
                await UpdateAsync("Been banned, that user has not. Hmm.");
                return;
            }

            var configuration = DbContext.Configurations.First();
            var ejected = Context.Guild.Roles.First(x => x.Id == configuration.Ejected);
            await user.RemoveRoleAsync(ejected);
            ban.Active = false;
            await DbContext.SaveChangesAsync();

            try
            {
                await user.SendMessageAsync($"Hello there! Your ban from Efehan's Hangout has been revoked by {Context.User.Mention}. Your ship is now being put in Docking Bay 327...");
            }
            catch
            { }

            await UpdateAsync($"Been unbanned from Efehan's Hangout, {user.Mention} has.");

            var cancellationTokenSource = new CancellationTokenSource();
            OnboardingService.StartingProcedures.Add(user.Id, cancellationTokenSource);
            await _onboardingService.StartOnboarding(user, cancellationTokenSource.Token);
        }

        [RequireUserPermission(GuildPermission.Administrator)]
        [SlashCommand("lockdown", "Toggle lockdown mode, determining whether or not people can board.")]
        public async Task Lockdown([Summary("enable", "Whether or not to enable lockdown mode.")] [Choice("true", "true"), Choice("false", "false")] string input)
        {
            await DeferAsync();
            bool shouldEnable = input == "true";

            var configuration = DbContext.Configurations.First();
            if ((configuration.Lockdown && shouldEnable) || (!configuration.Lockdown && !shouldEnable))
            {
                await UpdateAsync($"The ship is {(shouldEnable ? "already" : "not")} in lockdown mode.");
                return;
            }

            configuration.Lockdown = shouldEnable;
            await DbContext.SaveChangesAsync();

            await UpdateAsync($"Lockdown mode has been {(shouldEnable ? "enabled" : "disabled")}, new members will be {(shouldEnable ? "thrown into space" : "welcomed with cookies")}.");

            if (shouldEnable)
                return;

            var ejected = Context.Guild.Roles.First(x => x.Id == configuration.Ejected);
            var unidentified = Context.Guild.Roles.First(x => x.Id == configuration.Unidentified);

            // Loop over all users that are unidentified (joined during the lockdown).
            foreach (var user in unidentified.Members)
            {
                await user.RemoveRoleAsync(unidentified);
                var ban = DbContext.Infractions.FirstOrDefault(x => x.Active && x.Type == InfractionType.Ban && x.User == user.Id);

                // Check if the user is banned.
                if (ban != null)
                {
                    // Check if the ban has expired. If true, set the infraction to inactive and continue. If false, stop any further actions.
                    if (ban.ExpiresOn != default(DateTime) && ban.ExpiresOn <= DateTime.Now)
                    {
                        ban.Active = false;
                        await DbContext.SaveChangesAsync();
                    }
                    else
                    {
                        continue;
                    }
                }
                    
                await user.RemoveRoleAsync(ejected);

                try
                {
                    await user.SendMessageAsync("Hello there, stranger! Efehan's Hangout is no longer in lockdown and thus, you can board. Your ship will be landed momentarily...");
                }
                catch
                { }

                var cancellationTokenSource = new CancellationTokenSource();
                OnboardingService.StartingProcedures.Add(user.Id, cancellationTokenSource);
                await _onboardingService.StartOnboarding(user, cancellationTokenSource.Token);

                // Add a small delay to prevent getting rate limited.
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }
    }
}
