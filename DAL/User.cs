using System.Collections.Generic;

namespace DAL
{
    public sealed class User : DbEntry
    {
        public int TelegramUserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }

        public ICollection<Subscription> Subscriptions { get; set; }

        public User()
        {
            Subscriptions = new List<Subscription>();
        }

        public override string ToString()
        {
            return
                (string.IsNullOrEmpty(FirstName) ? "" : FirstName) +
                (string.IsNullOrEmpty(LastName) ? "" : $" {LastName}") +
                (string.IsNullOrEmpty(Username) ? "" : $" ({Username})");
        }
    }
}
