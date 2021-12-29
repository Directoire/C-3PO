using C_3PO.Data.Context;
using C_3PO.Services;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace C_3PO.Handlers
{
    internal class ButtonExecutedHandler : DiscordClientService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly OnboardingService _onboardingService;

        public ButtonExecutedHandler(
            DiscordSocketClient client,
            ILogger<DiscordClientService> logger,
            IServiceProvider serviceProvider,
            OnboardingService onboardingService)
            : base(client, logger)
        {
            _serviceProvider = serviceProvider;
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
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var configuration = dbContext.Configurations.First();
                var guild = Client.GetGuild(configuration.Id);
                var user = guild.GetUser(component.User.Id);

                // Check whether the user that pressed the button is part of an onboarding procedure.
                var onboarding = dbContext.Onboardings.FirstOrDefault(x => x.Id == component.User.Id);
                if (onboarding != null && onboarding.Channel == component.Channel.Id)
                {
                    await _onboardingService.ProcessButton(dbContext, component);
                    return;
                }

                if(ulong.TryParse(component.Data.CustomId, out ulong parsedId))
                {
                    await component.DeferAsync();
                    // Determine whether the parsedId is a category or notification role.
                    if (dbContext.Categories.All(x => x.Id != parsedId) &&
                        dbContext.NotificationRoles.All(x => x.Id != parsedId))
                        return;

                    // If the parsedId is a category, toggle the role of the category for the user.
                    if (dbContext.Categories.Any(x => x.Id == parsedId))
                    {
                        var category = dbContext.Categories.Include(x => x.NotificationRole).First(x => x.Id == parsedId);
                        var categoryRole = guild.GetRole(category.Role);
                        if (user.Roles.Contains(categoryRole))
                        {
                            await user.RemoveRoleAsync(categoryRole);

                            // Check if the category has a notification role
                            if (category.NotificationRole == null)
                                return;

                            // Get the notification role for the category
                            var categoryNotificationRole = guild.GetRole(category.NotificationRole.Id);

                            // Check if the user has the notification role of the category
                            if (!user.Roles.Contains(categoryNotificationRole))
                                return;

                            // Remove the category's notification role from the user
                            await user.RemoveRoleAsync(categoryNotificationRole);
                        }
                        else
                            await user.AddRoleAsync(categoryRole);

                        return;
                    }

                    // If the parsedId is a notificationRole, toggle the notification role for the user.
                    var role = guild.GetRole(parsedId);

                    var notificationRole = dbContext.NotificationRoles.First(x => x.Id == parsedId);
                    if (notificationRole.CategoryId != null)
                    {
                        var category = dbContext.Categories.Find(notificationRole.CategoryId);
                        if (user.Roles.All(x => x.Id != category.Role))
                        {
                            await component.FollowupAsync("You cannot join this notification role as you're not within the related category.", ephemeral: true);
                            return;
                        }
                    }

                    if (user.Roles.Contains(role))
                        await user.RemoveRoleAsync(role);
                    else
                        await user.AddRoleAsync(role);
                    
                    return;
                }
            });
            return Task.CompletedTask;
        }
    }
}
