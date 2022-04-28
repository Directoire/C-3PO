using C_3PO.Common;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace C_3PO.Services
{
    public class HeartbeatService : DiscordClientService
    {
        private readonly AppConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public HeartbeatService(
            DiscordSocketClient client,
            ILogger<DiscordClientService> logger,
            AppConfiguration configuration,
            IHttpClientFactory httpClientFactory)
            : base(client, logger)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Client.WaitForReadyAsync(stoppingToken);
            await Heartbeat();
        }

        private Task Heartbeat()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        using var client = _httpClientFactory.CreateClient();

                        var request = new HttpRequestMessage
                        {
                            Method = HttpMethod.Head,
                            RequestUri = new Uri(_configuration.HeartbeatUrl),
                        };

                        await client.SendAsync(request);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex.ToString());
                    }

                    await Task.Delay(TimeSpan.FromMinutes(5));
                }
            });
            return Task.CompletedTask;
        }
    }
}