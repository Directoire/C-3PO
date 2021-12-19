using C_3PO.Common;
using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace C_3PO.Services;

internal class InteractionHandler : DiscordClientService
{
    private readonly IServiceProvider _provider;
    private readonly InteractionService _interactionService;
    private readonly AppConfiguration _configuration;

    public InteractionHandler(
        DiscordSocketClient client,
        ILogger<DiscordClientService> logger,
        IServiceProvider provider,
        InteractionService interactionService,
        AppConfiguration configuration)
        : base(client, logger)
    {
        _provider = provider;
        _interactionService = interactionService;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Client.InteractionCreated += HandleInteraction;

        await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);
        await Client.WaitForReadyAsync(stoppingToken);

        await _interactionService.RegisterCommandsToGuildAsync(_configuration.Guild);
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