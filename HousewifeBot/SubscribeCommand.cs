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
            string serialTitle;
            if (string.IsNullOrEmpty(Arguments))
            {
                TelegramApi.SendMessage(Message.From, "Введите название сериала");
                serialTitle = TelegramApi.WaitForMessage(Message.From).Text;
            }
            else
            {
                serialTitle = Arguments;
            }

            string response;
            using (AppDbContext db = new AppDbContext())
            {
                Show show = db.Shows.FirstOrDefault(s => s.Title.ToLower() == serialTitle.ToLower() ||
                                                         s.OriginalTitle.ToLower() == serialTitle.ToLower());
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
                        response = $"Вы уже подписаны на сериал '{show.Title}'";
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
                        response = $"Вы подписались на сериал '{show.Title}'";
                    }
                }
                else
                {
                        response = $"Сериал '{serialTitle}' не найден";
                }
                db.SaveChanges();
            }

            TelegramApi.SendMessage(Message.From, response);
            return true;
        }
    }
}
