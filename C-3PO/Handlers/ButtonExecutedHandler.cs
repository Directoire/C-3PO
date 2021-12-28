using C_3PO.Assets;
using C_3PO.Common;
using C_3PO.Data.Context;
using C_3PO.Services;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace C_3PO.Handlers
{
    internal class ButtonExecutedHandler : DiscordClientService
    {
        private readonly AppDbContext _dbContext;
        private readonly OnboardingService _onboardingService;

        public ButtonExecutedHandler(
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
            Client.ButtonExecuted += Client_ButtonExecuted;
            return Task.CompletedTask;
        }

        private Task Client_ButtonExecuted(SocketMessageComponent component)
        {
            Task.Run(async () =>
            {
                // Check whether the user that pressed the button is part of an onboarding procedure.
                var onboarding = _dbContext.Onboardings.FirstOrDefault(x => x.Id == component.User.Id);
                
                if (onboarding != null && onboarding.Channel == component.Channel.Id)
                {
                    await _onboardingService.ProcessButton(component);
                    return;
                }
            });
            return Task.CompletedTask;
        }
    }
}
