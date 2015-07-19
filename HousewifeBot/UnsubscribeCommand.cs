using System.Linq;
using System.Threading;
using DAL;
using Telegram;
using User = DAL.User;

namespace HousewifeBot
{
    public class UnsubscribeCommand : Command
    {
        public override bool Execute()
        {
            Message message = TelegramApi.WaitForMessage(Message.From);
            string serialTitle = message.Text;
            string response = string.Empty;

            using (var db = new AppDbContext())
            {
                do
                {
                    Show serial = db.Shows.FirstOrDefault(s =>
                        s.Title.ToLower() == serialTitle.ToLower() ||
                        s.OriginalTitle.ToLower() == serialTitle.ToLower()
                        );

                    if (serial == null)
                    {
                        response = $"Сериал '{serialTitle}' не найден";
                        break;
                    }

                    User user = db.Users.FirstOrDefault(u => u.TelegramUserId == message.From.Id);
                    if (user == null)
                    {
                        response = "Вы не подписаны ни на один сериал";
                        break;
                    }

                    Subscription subscription = db.Subscriptions.FirstOrDefault(
                        s => s.User.Id == user.Id && s.Show.Id == serial.Id
                        );
                    if (subscription == null)
                    {
                        response = $"Вы не подписаны на сериал '{serial.Title}'";
                        break;
                    }

                    db.Notifications.RemoveRange(
                        db.Notifications.Where(n => n.Subscription.Id == subscription.Id)
                        );

                    db.Subscriptions.Remove(subscription);
                    response = $"Вы отписались от сериала '{serialTitle}'";
                } while (false);
                db.SaveChanges();
            }
            TelegramApi.SendMessage(message.From, response);
            return true;
        }
    }
}
