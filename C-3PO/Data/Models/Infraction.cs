using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_3PO.Data.Models
{
    public class Infraction
    {
        public int Id { get; set; }
        public ulong User { get; set; }
        public ulong Moderator { get; set; }
        public InfractionType Type { get; set; }
        public DateTime IssuedOn { get; set; } = DateTime.Now;
        public DateTime ExpiresOn { get; set; }
        public bool Active { get; set; } = true;
        public string? Reason { get; set; }
    }

    public enum InfractionType
    {
        Warn,
        Kick,
        Ban
    }
}
