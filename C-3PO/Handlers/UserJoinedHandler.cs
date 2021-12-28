﻿using C_3PO.Data.Context;
using C_3PO.Data.Models;
using C_3PO.Services;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace C_3PO.Handlers
{
    public class UserJoinedHandler : DiscordClientService
    {
        private readonly AppDbContext _dbContext;
        private readonly OnboardingService _onboardingService;

        public UserJoinedHandler(
            DiscordSocketClient client,
            ILogger<DiscordClientService> logger,
            AppDbContext dbContext,
            OnboardingService onboardingService)
            : base(client, logger)
        {
            _dbContext = dbContext;
            _onboardingService = onboardingService;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Client.UserJoined += Client_UserJoined;
            return Task.CompletedTask;
        }

        private Task Client_UserJoined(SocketGuildUser user)
        {
            Task.Run(async () =>
            {
                var configuration = _dbContext.Configurations.First();
                var ejected = Client.GetGuild(configuration.Id).GetRole(configuration.Ejected);

                var ban = _dbContext.Infractions.FirstOrDefault(x => x.Active && x.Type == InfractionType.Ban && x.User == user.Id);

                // Check if the user is banned.
                if (ban != null)
                {
                    // Check if the ban has expired. If true, set the infraction to inactive and continue. If false, stop any further actions.
                    if (ban.ExpiresOn != default(DateTime) && ban.ExpiresOn <= DateTime.Now)
                    {
                        ban.Active = false;
                        await _dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        await user.AddRoleAsync(ejected);
                        return;
                    }
                }

                if (OnboardingService.StartingProcedures.Any(x => x.Key == user.Id))
                {
                    try
                    {
                        await (await user.CreateDMChannelAsync()).SendMessageAsync("You are joining and leaving Efehan's Hangout too quickly. Hence, you were automatically put back into space. Please wait for a while and then rejoin Efehan's Hangout to start the onboarding procedure.");
                        await user.AddRoleAsync(ejected);
                        return;
                    }
                    catch
                    {
                    }
                }

                var cancellationTokenSource = new CancellationTokenSource();
                OnboardingService.StartingProcedures.Add(user.Id, cancellationTokenSource);
                await _onboardingService.StartOnboarding(user, cancellationTokenSource.Token);
            });
            return Task.CompletedTask;
        }
    }
}