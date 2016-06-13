using System;
using System.Collections.Generic;
using System.Linq;
using DAL;

namespace HousewifeBot
{
    public class SubscribeAllCommand : Command
    {
        public override void Execute()
        {
            using (AppDbContext db = new AppDbContext())
            {
                Program.Logger.Debug($"{GetType().Name}: Searching user with TelegramId: {Message.From.Id} in database");
                var user = db.GetUserByTelegramId(Message.From.Id);
                if (user == null)
                {
                    user = new User
                    {
                        TelegramUserId = Message.From.Id,
                        FirstName = Message.From.FirstName,
                        LastName = Message.From.LastName,
                        Username = Message.From.Username
                    };

                    Program.Logger.Info($"{GetType().Name}: {user} is new User");
                    Program.Logger.Debug($"{GetType().Name}: Adding user {user} to database");
                    db.Users.Add(user);
                }
                else
                {
                    Program.Logger.Debug($"{GetType().Name}: User {user} is already exist");
                }

                Program.Logger.Debug($"{GetType().Name}: Subscribing {user} to all shows");
                var subscriptions = user.Subscriptions.ToList();
                foreach (Show show in db.Shows)
                {
                    Subscription subscription = new Subscription
                    {
                        User = user,
                        Show = show,
                        SubscriptionDate = DateTime.Now
                    };

                    if (subscriptions.Any(s => Equals(s.Show, show)))
                    {
                        continue;
                    }

                    db.Subscriptions.Add(subscription);
                }

                Program.Logger.Debug($"{GetType().Name}: Saving changes to database");
                db.SaveChanges();
            }

            Program.Logger.Debug($"{GetType().Name}: Sending response to {Message.From}");
            try
            {
                TelegramApi.SendMessage(Message.From, "Вы, братишка, подписаны на все сериалы");
            }
            catch (Exception e)
            {
                throw new Exception($"{GetType().Name}: An error occurred while sending response to {Message.From}", e);
            }

            Status = true;
        }
    }
}
