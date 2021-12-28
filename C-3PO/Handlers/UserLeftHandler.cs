using C_3PO.Data.Context;
using C_3PO.Services;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace C_3PO.Handlers
{
    public class UserLeftHandler : DiscordClientService
    {
        private readonly AppDbContext _dbContext;

        public UserLeftHandler(
            DiscordSocketClient client,
            ILogger<DiscordClientService> logger,
            AppDbContext dbContext)
            : base(client, logger)
        {
            _dbContext = dbContext;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Client.UserLeft += Client_UserLeft;
            return Task.CompletedTask;
        }

        private Task Client_UserLeft(SocketGuild guild, SocketUser user)
        {
            Task.Run(async() =>
            {
                // Check if the user just started the onboarding procedure.
                if (OnboardingService.StartingProcedures.Any(x => x.Key == user.Id))
                {
                    // Request the starting procedure to cancel.
                    OnboardingService.StartingProcedures.First(x => x.Key == user.Id).Value.Cancel();
                }
                else
                {
                    var onboarding = _dbContext.Onboardings.FirstOrDefault(x => x.Id == user.Id);

                    // If there isn't a starting onboarding procedure, check if there is one that has been progressed. If true, delete the channel and record.
                    if (onboarding != null)
                    {
                        await guild.GetTextChannel(onboarding.Channel).DeleteAsync();
                        _dbContext.Remove(onboarding);
                        await _dbContext.SaveChangesAsync();
                    }
                }
            });
            return Task.CompletedTask;
        }
    }
}
