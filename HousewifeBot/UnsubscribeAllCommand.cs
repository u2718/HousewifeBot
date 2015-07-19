using System.Linq;
using DAL;

namespace HousewifeBot
{
    public class UnsubscribeAllCommand : Command
    {
        public override bool Execute()
        {
            string response = string.Empty;
            using (var db = new AppDbContext())
            {
                do
                {
                    User user = db.Users.FirstOrDefault(s => s.TelegramUserId == Message.From.Id);
                    if (user == null)
                    {
                        response = $"Вы не подписаны ни на один сериал";
                        break;
                    }

                    var subscriptions = db.Subscriptions.Where(s => s.User.Id == user.Id);
                    if (!subscriptions.Any())
                    {
                        response = $"Вы не подписаны ни на один сериал";
                        break;
                    }

                    db.Notifications.RemoveRange(
                        db.Notifications.Where(n => subscriptions.Any(s => s.Id == n.Subscription.Id))
                        );
                    db.Subscriptions.RemoveRange(subscriptions);

                    response = $"Вы отписались от всех сериалов";
                } while (false);
                db.SaveChanges();
            }

            TelegramApi.SendMessage(Message.From, response);
            return true;
        }
    }
}
