namespace C_3PO.Data.Models
{
    public class Configuration
    {
        /// <summary>
        /// ID of the guild.
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// The ID of the outer rim category, used to put onboarding channels under.
        /// </summary>
        public ulong OuterRim { get; set; }

        /// <summary>
        /// The ID of the onboarding role.
        /// </summary>
        public ulong Onboarding { get; set; }

        /// <summary>
        /// The ID of the hangar channel, used to welcome new members.
        /// </summary>
        public ulong Hangar { get; set; }

        /// <summary>
        /// The ID of the rules channel. The first message will be used to read the rules from.
        /// </summary>
        public ulong Rules { get; set; }

        /// <summary>
        /// The ID of the ejected role, given to users when they fail the onboarding process or get banned.
        /// </summary>
        public ulong Ejected { get; set; }

        /// <summary>
        /// The ID of the civilian role, given to users when they've successfully finished the onboarding procedure.
        /// </summary>
        public ulong Civilian { get; set; }

        /// <summary>
        /// The ID of the logs channel.
        /// </summary>
        public ulong Logs { get; set; }

        /// <summary>
        /// A bool indicating whether or not the lockdown mode is enabled.
        /// </summary>
        public bool Lockdown { get; set; }

        /// <summary>
        /// The ID of the unidentified role, assigned to users during the lockdown.
        /// </summary>
        public ulong Unidentified { get; set; }

        /// <summary>
        /// The ID of the conduct channel, showing the rules of Efehan's Hangout.
        /// </summary>
        public ulong Conduct { get; set; }
        
        /// <summary>
        /// The ID of the loading bay channel, allowing users to subscribe to categories and notification roles.
        /// </summary>
        public ulong LoadingBay { get; set; }
    }
}
