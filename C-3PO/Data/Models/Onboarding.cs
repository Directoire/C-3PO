namespace C_3PO.Data.Models
{
    public class Onboarding
    {
        /// <summary>
        /// ID of the user that is onboarding. Unique as the user can only have one onboarding per time.
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// ID of the onboarding channel.
        /// </summary>
        public ulong Channel { get; set; }

        /// <summary>
        /// ID of the message where the user is given the choice whether to cooperate or attack.
        /// </summary>
        public ulong ActionMessage { get; set; }

        /// <summary>
        /// ID of the message where the user is offered to join by Darth Vader. Only applicable if user chose to attack.
        /// </summary>
        public ulong OfferMessage { get; set; }

        /// <summary>
        /// ID of the message where the user is asked if they will comply with the rules.
        /// </summary>
        public ulong RulesMessage { get; set; }

        /// <summary>
        /// ID of the message where the user is asked which categories they'd like to join, if any.
        /// </summary>
        public ulong CategoriesMessage { get; set; }

        /// <summary>
        /// ID of the message where the user is asked which notifications they'd like to receive, if any.
        /// </summary>
        public ulong NotificationsMessage { get; set; }

        /// <summary>
        /// State of the onboarding process, indicating the progress of the user's onboarding process.
        /// </summary>
        public OnboardingState State { get; set; } = OnboardingState.Boarding;
    }

    public enum OnboardingState
    {
        Boarding,
        Cooperate,
        Attack,
        Offer,
        Rules,
        Categories,
        Notifications
    }
}
