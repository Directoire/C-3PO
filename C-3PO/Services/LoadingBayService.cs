using C_3PO.Assets;
using C_3PO.Data.Context;
using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_3PO.Services
{
    internal class LoadingBayService : DiscordClientService
    {
        private readonly IServiceProvider _serviceProvider;

        public LoadingBayService(
            DiscordSocketClient client,
            ILogger<DiscordClientService> logger,
            IServiceProvider serviceProvider)
            : base(client, logger)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Client.WaitForReadyAsync(stoppingToken);
            await SyncLoadingBay();
        }

        private Task SyncLoadingBay()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var configuration = dbContext.Configurations.First();
                    var guild = Client.GetGuild(configuration.Id);
                    var loadingBay = guild.GetTextChannel(configuration.LoadingBay);
                    var message = (await loadingBay.GetMessagesAsync().FlattenAsync()).LastOrDefault();

                    var componentBuilder = new ComponentBuilder();
                    int row = 0;
                    foreach (var category in dbContext.Categories.Include(x => x.NotificationRole))
                    {
                        var name = guild.GetCategoryChannel(category.Id).Name;
                        componentBuilder.WithButton($"{name}", category.Id.ToString(), ButtonStyle.Secondary, row: row);
                        componentBuilder.WithButton(
                            $"{guild.GetRole(category.NotificationRole.Id).Name}",
                            category.NotificationRole.Id.ToString(),
                            ButtonStyle.Secondary, row: row);
                        row++;
                    }

                    foreach (var notificationRole in dbContext.NotificationRoles.Where(x => x.CategoryId == null))
                    {
                        componentBuilder.WithButton(
                            $"{guild.GetRole(notificationRole.Id).Name}",
                            notificationRole.Id.ToString(),
                            ButtonStyle.Secondary, row: row);
                    }

                    if (message == null)
                    {
                        await loadingBay.SendMessageAsync("Hello there, civilian. Missed a category or notification role or getting tired of one? I got you covered. Press a button to toggle between roles.", components: componentBuilder.Build());
                        await loadingBay.SendMessageAsync(AppAssets.GIFs.DroidCookies);
                        return;
                    }
                    else
                    {
                        await ((IUserMessage)message).ModifyAsync(x => x.Components = componentBuilder.Build());
                    }

                    await Task.Delay(TimeSpan.FromHours(1));
                }
            });
            return Task.CompletedTask;
        }
    }
}