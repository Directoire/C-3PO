using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_3PO.Services
{
    internal class StatusService : DiscordClientService
    {
        public StatusService(
            DiscordSocketClient client,
            ILogger<DiscordClientService> logger)
            : base(client, logger)
        {
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Client.WaitForReadyAsync(stoppingToken);
            await WatchStatus();
        }

        private Task WatchStatus()
        {
            Task.Run(async () =>
            {
                var lastQuote = string.Empty;

                while (true)
                {
                    string[] quotes =
                    {
                        "Well, if droids could think, there’d be none of us here, would there?",
                        "I find your lack of faith disturbing.",
                        "Let the wookie win.",
                        "Do or do not. There is no try.",
                        "The garbage’ll do!",
                        "Your focus determines your reality.",
                        "Fear is the path to the dark side.",
                        "In my experience there is no such thing as luck.",
                        "Help me, Obi-Wan Kenobi. You’re my only hope.",
                        "Oh, my dear friend. How I’ve missed you.",
                        "Size matters not. Look at me. Judge me by my size, do you?",
                        "If you strike me down, I shall become more powerful than you can possibly imagine.",
                        "A long time ago in a galaxy far, far away.",
                        "Never tell me the odds!",
                        "No. I am your father.",
                        "There’s always a bigger fish.",
                        "Power! Unlimited power!",
                        "You were my brother, Anakin. I loved you.",
                        "Hope.",
                    };

                    if (!string.IsNullOrEmpty(lastQuote))
                    {
                        quotes = quotes.Where(x => x != lastQuote).ToArray();
                    }

                    string nextQuote = quotes[new Random().Next(0, quotes.Length)];
                    lastQuote = nextQuote;

                    if (DateTime.Now.Day == 1 && DateTime.Now.Month == 1)
                        nextQuote = "Happy New Year!";

                    await Client.SetGameAsync(nextQuote, null, ActivityType.Playing);

                    await Task.Delay(TimeSpan.FromMinutes(10));
                }
            });
            return Task.CompletedTask;
        }
    }
}
