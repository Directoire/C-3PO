﻿using System.Reflection;
using C_3PO.Common;
using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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

        _interactionService.SlashCommandExecuted += SlashCommandExecuted;

        await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _provider);
        await Client.WaitForReadyAsync(stoppingToken);

        await _interactionService.RegisterCommandsToGuildAsync(_configuration.Guild);
    }

    private Task SlashCommandExecuted(SlashCommandInfo commandInfo, IInteractionContext context, IResult result)
    {
        if (!result.IsSuccess)
        {
            switch (result.Error)
            {
                case InteractionCommandError.UnmetPrecondition:
                    // implement
                    break;
                case InteractionCommandError.UnknownCommand:
                    // implement
                    break;
                case InteractionCommandError.BadArgs:
                    // implement
                    break;
                case InteractionCommandError.Exception:
                    // implement
                    break;
                case InteractionCommandError.Unsuccessful:
                    // implement
                    break;
                default:
                    break;
            }
        }
        return Task.CompletedTask;
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