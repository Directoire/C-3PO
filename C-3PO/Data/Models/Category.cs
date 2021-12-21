namespace C_3PO.Data.Models
{
    public class Category
    {
        /// <summary>
        /// The ID of the category, linked to the Discord category channel.
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// The ID of the role for this category.
        /// </summary>
        public ulong Role { get; set; }

        /// <summary>
        /// The ID of the feed channel.
        /// </summary>
        public ulong? Feed { get; set; }

        /// <summary>
        /// URL to the RSS feed, if any. Updates will be posted in Feed.
        /// </summary>
        public string? RSS { get; set; }

        /// <summary>
        /// An optional role that can be pinged for posts in Feed.
        /// </summary>
        public virtual NotificationRole? NotificationRole { get; set; }
    }
}
