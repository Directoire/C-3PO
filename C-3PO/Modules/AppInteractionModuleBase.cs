using C_3PO.Data.Context;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;

namespace C_3PO.Modules
{
    public abstract class AppInteractionModuleBase : InteractionModuleBase<SocketInteractionContext>
    {
        public readonly AppDbContext DbContext;
        public ILogger<AppInteractionModuleBase> Logger;

        public AppInteractionModuleBase(
            AppDbContext dbContext,
            ILogger<AppInteractionModuleBase> logger)
        {
            DbContext = dbContext;
            Logger = logger;
        }

        /// <summary>
        /// Update an interaction after it has been deferred.
        /// </summary>
        /// <param name="content">The new content of the message.</param>
        /// <returns></returns>
        public async Task<IUserMessage> UpdateAsync(string content)
        {
            return await Context.Interaction.ModifyOriginalResponseAsync(x => x.Content = content);
        }

        public async Task<IUserMessage> UpdateAsync(Embed embed)
        {
            return await Context.Interaction.ModifyOriginalResponseAsync(x => x.Embed = embed);
        }
    }
}
