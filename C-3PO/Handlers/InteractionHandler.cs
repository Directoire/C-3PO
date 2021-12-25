using C_3PO.Data.Context;
using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace C_3PO.Handlers;

internal class InteractionHandler : DiscordClientService
{
    private readonly IServiceProvider _provider;
    private readonly InteractionService _interactionService;
    private readonly AppDbContext _dbContext;

    public InteractionHandler(
        DiscordSocketClient client,
        ILogger<DiscordClientService> logger,
        IServiceProvider provider,
        InteractionService interactionService,
        AppDbContext dbContext)
        : base(client, logger)
    {
        _provider = provider;
        _interactionService = interactionService;
        _dbContext = dbContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Client.InteractionCreated += HandleInteraction;

        await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);
        await Client.WaitForReadyAsync(stoppingToken);

        var guild = _dbContext.Configurations.First().Id;

        await _interactionService.RegisterCommandsToGuildAsync(guild);
    }

    private async Task HandleInteraction(SocketInteraction socketInteraction)
    {
        try
        {
            var ctx = new SocketInteractionContext(Client, socketInteraction);
            await _interactionService.ExecuteCommandAsync(ctx, _provider);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Exception occurred whilst attempting to handle interaction.");

            if (socketInteraction.Type == InteractionType.ApplicationCommand)
            {
                var msg = await socketInteraction.GetOriginalResponseAsync();
                await msg.DeleteAsync();
            }
        }
    }
}