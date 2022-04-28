using C_3PO.Common;
using C_3PO.Data.Context;
using C_3PO.Data.Models;
using C_3PO.Services;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace C_3PO.Handlers
{
    public class UserJoinedHandler : DiscordClientService
    {
        public static bool Lockdown = false;

        private readonly IServiceProvider _serviceProvider;
        private readonly OnboardingService _onboardingService;
        private readonly LogsService _logsService;
        private readonly AppConfiguration _configuration;

        public UserJoinedHandler(
            DiscordSocketClient client,
            ILogger<DiscordClientService> logger,
            IServiceProvider serviceProvider,
            OnboardingService onboardingService,
            LogsService logsService,
            AppConfiguration configuration)
            : base(client, logger)
        {
            _serviceProvider = serviceProvider;
            _onboardingService = onboardingService;
            _logsService = logsService;
            _configuration = configuration;
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
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var ejected = Client.GetGuild(_configuration.Guild).GetRole(_configuration.Roles.Ejected);
                var unidentified = Client.GetGuild(_configuration.Guild).GetRole(_configuration.Roles.Unidentified);

                var ban = dbContext.Infractions.FirstOrDefault(x => x.Active && x.Type == InfractionType.Ban && x.User == user.Id);

                // Check if the user is banned.
                if (ban != null)
                {
                    // Check if the ban has expired. If true, set the infraction to inactive and continue. If false, stop any further actions.
                    if (ban.ExpiresOn != default(DateTime) && ban.ExpiresOn <= DateTime.Now)
                    {
                        ban.Active = false;
                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        await user.AddRoleAsync(ejected);
                        return;
                    }
                }

                if (Lockdown)
                {
                    await user.AddRoleAsync(ejected);
                    await user.AddRoleAsync(unidentified);
                    return;
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

                await _logsService.Log($"{user.Mention} ({user}) joined the server.");
                var cancellationTokenSource = new CancellationTokenSource();
                OnboardingService.StartingProcedures.Add(user.Id, cancellationTokenSource);
                await _onboardingService.StartOnboarding(user, cancellationTokenSource.Token);
            });
            return Task.CompletedTask;
        }
    }
}
