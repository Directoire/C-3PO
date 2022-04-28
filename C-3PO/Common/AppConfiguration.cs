using Microsoft.Extensions.Configuration;

namespace C_3PO.Common
{
    public class AppConfiguration
    {
        /// <summary>
        /// ID of the guild.
        /// </summary>
        public ulong Guild { get; set; }

        /// <summary>
        /// The token of the bot.
        /// </summary>
        public string Token { get; set; } = null!;

        /// <summary>
        /// The connection string for the MySQL database.
        /// </summary>
        public string Database { get; set; } = null!;

        /// <summary>
        /// The channels used by the bot.
        /// </summary>
        public Channels Channels { get; set; } = null!;

        /// <summary>
        /// The categories used by the bot.
        /// </summary>
        public Categories Categories { get; set; } = null!;

        /// <summary>
        /// The roles used by the bot.
        /// </summary>
        public Roles Roles { get; set; } = null!;

        /// <summary>
        /// The URL to send the heartbeat to for monitoring.
        /// </summary>
        public string HeartbeatUrl = null!;
    }

    public class Categories
    {
        /// <summary>
        /// The ID of the outer rim category, used to put onboarding channels under.
        /// </summary>
        public ulong OuterRim { get; set; }
    }

    public class Channels
    {
        /// <summary>
        /// The ID of the hangar channel, used to welcome new members.
        /// </summary>
        public ulong Hangar { get; set; }

        /// <summary>
        /// The ID of the rules channel. The first message will be used to read the rules from.
        /// </summary>
        public ulong Rules { get; set; }

        /// <summary>
        /// The ID of the logs channel.
        /// </summary>
        public ulong Logs { get; set; }

        /// <summary>
        /// The ID of the conduct channel, showing the rules of Efehan's Hangout.
        /// </summary>
        public ulong Conduct { get; set; }

        /// <summary>
        /// The ID of the loading bay channel, allowing users to subscribe to categories and notification roles.
        /// </summary>
        public ulong LoadingBay { get; set; }

        /// <summary>
        /// The ID of the programming support channel, used to automatically create threads.
        /// </summary>
        public ulong Support { get; set; }
    }

    public class Roles
    {
        /// <summary>
        /// The ID of the onboarding role.
        /// </summary>
        public ulong Onboarding { get; set; }

        /// <summary>
        /// The ID of the ejected role, given to users when they fail the onboarding process or get banned.
        /// </summary>
        public ulong Ejected { get; set; }

        /// <summary>
        /// The ID of the civilian role, given to users when they've successfully finished the onboarding procedure.
        /// </summary>
        public ulong Civilian { get; set; }

        /// <summary>
        /// The ID of the unidentified role, assigned to users during the lockdown.
        /// </summary>
        public ulong Unidentified { get; set; }
    }
}
