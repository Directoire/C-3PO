using C_3PO.Assets;
using C_3PO.Data.Context;
using C_3PO.Data.Models;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_3PO.Modules
{
    public class ModerationModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly AppDbContext _dbContext;

        public ModerationModule(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [RequireUserPermission(GuildPermission.KickMembers)]
        [SlashCommand("warn", "Warn a user with an optional reason.")]
        public async Task Warn(
            [Summary("user", "The user to warn.")] SocketGuildUser user,
            [Summary("reason", "The reason behind the warning.")] string? reason = null)
        {
            _dbContext.Add(new Infraction { User = user.Id, Reason = reason, Moderator = Context.User.Id, Type = InfractionType.Warn });
            await _dbContext.SaveChangesAsync();

            await RespondAsync($"Been warned, {user} has.");
            await ReplyAsync(AppAssets.GIFs.QuiGonPalm);
        }

        [RequireUserPermission(GuildPermission.KickMembers)]
        [SlashCommand("kick", "Kick a user and send them back into space.")]
        public async Task Kick(
            [Summary("user", "The user to kick.")] SocketGuildUser user,
            [Summary("reason", "The reason behind the kick.")] string? reason = null)
        {
            await user.RemoveRolesAsync(user.Roles.Where(x => !x.IsEveryone));
            var ejected = Context.Guild.GetRole(_dbContext.Configurations.First().Ejected);
            await user.AddRoleAsync(ejected);

            _dbContext.Add(new Infraction { User = user.Id, Reason = reason, Moderator = Context.User.Id, Type = InfractionType.Kick });
            await _dbContext.SaveChangesAsync();

            await RespondAsync($"Been thrown off the ship, {user} has.");
            await ReplyAsync(AppAssets.GIFs.TrooperSad);
        }

        [RequireUserPermission(GuildPermission.BanMembers)]
        [SlashCommand("ban", "Ban a user and send them back into space permanently.")]
        public async Task Ban(
            [Summary("user", "The user to ban.")] SocketGuildUser user,
            [Summary("reason", "The reason behind the ban.")] string? reason = null,
            [Summary("expiresOn", "The date and time when the ban expires.")] DateTime expiresOn = default(DateTime))
        {
            await user.RemoveRolesAsync(user.Roles.Where(x => !x.IsEveryone));
            var ejected = Context.Guild.GetRole(_dbContext.Configurations.First().Ejected);
            await user.AddRoleAsync(ejected);

            _dbContext.Add(new Infraction { User = user.Id, Reason = reason, Moderator = Context.User.Id, Type = InfractionType.Ban, ExpiresOn = expiresOn });
            await _dbContext.SaveChangesAsync();

            await RespondAsync($"Been banned from Efehan's Hangout, {user} has.");
            await ReplyAsync(AppAssets.GIFs.HanSad);
        }
    }
}
