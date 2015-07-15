using System;
using System.Linq;
using System.Threading;
using DAL;
using Telegram;
using User = DAL.User;

namespace HousewifeBot
{
    public class SubscribeCommand : Command
    {
        public override bool Execute()
        {

            do
            {
                Thread.Sleep(200);
            } while (TelegramApi.Updates[Message.From].IsEmpty);

            Message message;
            TelegramApi.Updates[Message.From].TryDequeue(out message);
            string serialTitle = message.Text;

            string response;
            using (AppDbContext db = new AppDbContext())
            {
                Show show = db.Shows.FirstOrDefault(s => s.Title.ToLower() == serialTitle.ToLower());
                if (show != null)
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

                    if (user.Subscriptions.Any(s => s.Show.Id == show.Id))
                    {
                        response = $"You're already subscribed to '{show.Title}'";
                    }
                    else
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
                        response = $"You're subscribed to '{show.Title}'";
                    }
                }
                else
                {
                        response = $"'{serialTitle}' not found";
                }
                db.SaveChanges();
            }

            TelegramApi.SendMessage(Message.From.Id, response);
            return true;
        }
    }
}
