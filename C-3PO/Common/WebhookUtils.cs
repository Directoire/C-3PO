using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace C_3PO.Common
{
    public static class WebhookUtils
    {
        public static async Task<DiscordWebhookClient> GetOrCreateWebhookAsync(this IChannel channel, string username, string avatarPath, HttpClient httpClient)
        {
            Stream avatar;
            if (avatarPath.StartsWith("https://"))
                avatar = await httpClient.GetStreamAsync(avatarPath);
            else
                avatar = new MemoryStream(File.ReadAllBytes(avatarPath));

            var textChannel = (ITextChannel)channel;

            var webhook = (await textChannel.GetWebhooksAsync())?.FirstOrDefault(x => x.Name == username);
            if (webhook != null)
                return new DiscordWebhookClient(webhook);
            else
            {
                var newWebhook = await textChannel.CreateWebhookAsync(username, avatar, new RequestOptions { RetryMode = RetryMode.AlwaysFail});
                return new DiscordWebhookClient(newWebhook);
            }
        }
    }
}
