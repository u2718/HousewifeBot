using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL
{
    public class User : DbEntry
    {
        public int TelegramUserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTimeOffset? DateCreated { get; set; }
        public override string ToString()
        {
            return
                (string.IsNullOrEmpty(FirstName) ? "" : FirstName) +
                (string.IsNullOrEmpty(LastName) ? "" : $" {LastName}") +
                (string.IsNullOrEmpty(Username) ? "" : $" ({Username})");
        }
    }
}
