using C_3PO.Data.Context;
using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.ServiceModel.Syndication;
using System.Xml;

namespace C_3PO.Services
{
    public class FeedsService : DiscordClientService
    {
        private readonly IServiceProvider _serviceProvider;

        public FeedsService(
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
            await WatchFeeds();
        }

        private Task WatchFeeds()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    foreach (var category in dbContext.Categories.Include(x => x.NotificationRole))
                    {
                        if (category.Feed == null)
                            continue;

                        using var reader = XmlReader.Create(category.RSS!);
                        var feed = SyndicationFeed.Load(reader);

                        var channel = Client.GetChannel(category.Feed.Value) as ITextChannel;
                        var lastPost = (await channel!.GetMessagesAsync(5).FlattenAsync()).Where(x => x.Author.Id == Client.CurrentUser.Id).FirstOrDefault();
                        if (lastPost != null && feed.Items.First().PublishDate.UtcDateTime < lastPost.CreatedAt)
                            continue;

                        var item = feed.Items.First();
                        await ((SocketTextChannel)channel).SendMessageAsync($"<@&{category.NotificationRole!.Id}> | **{item.Title.Text}**\n\n{item.Links.First().Uri}");
                    }

                    await Task.Delay(TimeSpan.FromMinutes(5));
                }
            });
            return Task.CompletedTask;
        }
    }
}
