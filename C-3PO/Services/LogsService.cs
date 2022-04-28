using C_3PO.Assets;
using C_3PO.Common;
using C_3PO.Data.Context;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace C_3PO.Services
{
    public class LogsService
    {
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LogsService> _logger;
        private readonly AppConfiguration _configuration;

        public LogsService(DiscordSocketClient client,
            IServiceProvider serviceProvider,
            ILogger<LogsService> logger,
            AppConfiguration configuration)
        {
            _client = client;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task Log(string message)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var channel = _client.GetGuild(_configuration.Guild).GetTextChannel(_configuration.Channels.Logs);
                var embed = new EmbedBuilder()
                    .WithDescription(message)
                    .WithColor(Colours.Primary)
                    .Build();

                await channel.SendMessageAsync(embed: embed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        }

        public async Task Log(Embed embed)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var channel = _client.GetGuild(_configuration.Guild).GetTextChannel(_configuration.Channels.Logs);
                await channel.SendMessageAsync(embed: embed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        }
    }
}
