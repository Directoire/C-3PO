using C_3PO.Common;
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
    private readonly AppConfiguration _configuration;

    public InteractionHandler(
        DiscordSocketClient client,
        ILogger<DiscordClientService> logger,
        IServiceProvider provider,
        InteractionService interactionService,
        AppDbContext dbContext,
        AppConfiguration configuration)
        : base(client, logger)
    {
        _provider = provider;
        _interactionService = interactionService;
        _dbContext = dbContext;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Client.InteractionCreated += HandleInteraction;
        _interactionService.SlashCommandExecuted += SlashCommandExecuted;

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

    private async Task SlashCommandExecuted(SlashCommandInfo command, IInteractionContext context, IResult result)
    {
        if (result.IsSuccess)
            return;

        switch (result.Error)
        {
            case InteractionCommandError.UnmetPrecondition:
                await context.Interaction.RespondAsync("Beep-boop... you are not allowed to run this command.", ephemeral: true);
                break;
            default:
                await context.Interaction.RespondAsync("Beep-boop... something went wrong. Please try again later.", ephemeral: true);
                break;
        }
    }
}