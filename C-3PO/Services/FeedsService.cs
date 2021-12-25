using C_3PO.Common;
using C_3PO.Data.Context;
using Discord.Addons.Hosting;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ServiceModel.Syndication;
using System.Xml;

namespace C_3PO.Services
{
    public class FeedsService : DiscordClientService
    {
        private readonly AppDbContext _dbContext;
        private DateTime _lastRunAt = DateTime.UtcNow;

        public FeedsService(
            DiscordSocketClient client,
            ILogger<DiscordClientService> logger,
            AppDbContext dbContext)
            : base(client, logger)
        {
            _dbContext = dbContext;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Client.Ready += WatchFeeds;
            return Task.CompletedTask;
        }

        private Task WatchFeeds()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    foreach (var category in _dbContext.Categories.Include(x => x.NotificationRole))
                    {
                        if (category.Feed == null)
                            continue;

                        using var reader = XmlReader.Create(category.RSS!);
                        var feed = SyndicationFeed.Load(reader);

                        if (feed.Items.First().PublishDate.UtcDateTime < _lastRunAt)
                            continue;

                        var channel = Client.GetChannel(category.Feed.Value);
                        var item = feed.Items.First();
                        await ((SocketTextChannel)channel).SendMessageAsync($"<@&{category.NotificationRole!.Id}> | **{item.Title.Text}**\n\n{item.Links.First().Uri}");
                    }

                    _lastRunAt = DateTime.UtcNow;
                    await Task.Delay(TimeSpan.FromMinutes(5));
                }
            });
            return Task.CompletedTask;
        }
    }
}
