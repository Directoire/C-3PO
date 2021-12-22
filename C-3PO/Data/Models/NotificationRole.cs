using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_3PO.Data.Models
{
    public class NotificationRole
    {
        /// <summary>
        /// The ID of the role.
        /// </summary>
        public ulong Id { get; set; }

        /// <summary>
        /// The ID of the category that the notification role is linked to, if any.
        /// </summary>
        public ulong? CategoryId { get; set; } = 0;

        public virtual Category? Category { get; set; }
    }
}
