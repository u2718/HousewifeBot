using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL;

namespace HousewifeBot
{
    public class SubscribeAllCommand : Command
    {
        public override bool Execute()
        {
            using (AppDbContext db = new AppDbContext())
            {
                User user = db.Users.FirstOrDefault(u => u.TelegramUserId == Message.From.Id);
                bool newUser = false;
                if (user == null)
                {
                    user = new User
                    {
                        TelegramUserId = Message.From.Id,
                        FirstName = Message.From.FirstName,
                        LastName = Message.From.LastName,
                        Username = Message.From.Username
                    };
                    newUser = true;
                }

                foreach (Show show in db.Shows)
                {
                    Subscription subscription = new Subscription
                    {
                        User = user,
                        Show = show,
                        SubscriptionDate = DateTime.Now
                    };

                    if (newUser)
                    {
                        user.Subscriptions.Add(subscription);
                        db.Users.Add(user);
                    }
                    else
                    {
                        db.Subscriptions.Add(subscription);
                    }
                }

                db.SaveChanges();
            }
            TelegramApi.SendMessage(Message.From.Id, "Вы, братишка, подписаны на все сериалы");
            return true;
        }
    }
}
