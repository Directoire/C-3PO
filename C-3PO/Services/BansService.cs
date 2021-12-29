using C_3PO.Data.Context;
using C_3PO.Data.Models;
using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace C_3PO.Services
{
    public class BansService : DiscordClientService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly OnboardingService _onboardingService;

        public BansService(
            DiscordSocketClient client,
            ILogger<DiscordClientService> logger,
            IServiceProvider serviceProvider,
            OnboardingService onboardingService)
            : base(client, logger)
        {
            _serviceProvider = serviceProvider;
            _onboardingService = onboardingService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Client.WaitForReadyAsync(stoppingToken);
            await UpdateBans();
        }

        private Task UpdateBans()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var configuration = dbContext.Configurations.First();
                    var guild = Client.GetGuild(configuration.Id);
                    var ejected = guild.Roles.First(x => x.Id == configuration.Ejected);

                    foreach (var ban in dbContext.Infractions.Where(x => x.Active && x.Type == InfractionType.Ban && x.ExpiresOn != default(DateTime)))
                    {
                        if (ban.ExpiresOn > DateTime.Now)
                            continue;

                        var user = guild.GetUser(ban.User);

                        if (user != null)
                        {
                            await user.RemoveRoleAsync(ejected);

                            try
                            {
                                await user.SendMessageAsync($"Hello there! Your ban from Efehan's Hangout has been expired. Your ship is now being put in Docking Bay 327...");
                            }
                            catch
                            { }

                            var cancellationTokenSource = new CancellationTokenSource();
                            OnboardingService.StartingProcedures.Add(user.Id, cancellationTokenSource);
                            await _onboardingService.StartOnboarding(user, cancellationTokenSource.Token);
                        }

                        ban.Active = false;
                    }

                    await dbContext.SaveChangesAsync();
                    await Task.Delay(TimeSpan.FromMinutes(5));
                }
            });
            return Task.CompletedTask;
        }
    }
}
