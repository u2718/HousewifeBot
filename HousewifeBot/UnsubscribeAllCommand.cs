using System;
using System.Linq;
using DAL;
using Telegram;
using User = DAL.User;

namespace HousewifeBot
{
    public class UnsubscribeAllCommand : Command
    {
        public override void Execute()
        {
            string response;
            using (var db = new AppDbContext())
            {
                do
                {
                    Program.Logger.Debug($"{GetType().Name}: Searching user with TelegramId: {Message.From.Id} in database");
                    var user = db.GetUserByTelegramId(Message.From.Id);
                    if (user == null)
                    {
                        Program.Logger.Debug($"{GetType().Name}: User with TelegramId: {Message.From.Id} is not found");
                        response = "Вы не подписаны ни на один сериал";
                        break;
                    }

                    var subscriptions = user.Subscriptions;
                    if (!subscriptions.Any())
                    {
                        Program.Logger.Debug($"{GetType().Name}: {user} has no subscriptions");
                        response = "Вы не подписаны ни на один сериал";
                        break;
                    }

                    Program.Logger.Debug($"{GetType().Name}: Sending the confirmation message to {user}");
                    try
                    {
                        TelegramApi.SendMessage(Message.From, "Вы действительно хотите отписаться от всех сериалов?\n/yes /no");
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"{GetType().Name}: An error occurred while sending the confirmation message to {user}", e);
                    }

                    Program.Logger.Debug($"{GetType().Name}: Waiting for a message that contains confirmation");
                    Message msg;
                    try
                    {
                        msg = TelegramApi.WaitForMessage(Message.From);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"{GetType().Name}: An error occurred while waiting for a message that contains confirmation", e);
                    }

                    if (msg.Text.ToLower() != "/yes")
                    {
                        Program.Logger.Debug($"{GetType().Name}: {user} cancel command");
                        Status = true;
                    }

                    Program.Logger.Debug($"{GetType().Name}: Deleting notifications for all subscriptions");
                    foreach (var subscription in subscriptions)
                    {
                        db.Notifications.RemoveRange(db.Notifications.Where(n => n.Subscription.Id == subscription.Id));
                    }

                    Program.Logger.Debug($"{GetType().Name}: Deleting all subscriptions");
                    db.Subscriptions.RemoveRange(subscriptions);
                    response = "Вы отписались от всех сериалов";
                }
                while (false);

                Program.Logger.Debug($"{GetType().Name}: Saving changes to database");
                db.SaveChanges();
            }

            Program.Logger.Debug($"{GetType().Name}: Sending response to {Message.From}");
            try
            {
                TelegramApi.SendMessage(Message.From, response);
            }
            catch (Exception e)
            {
                throw new Exception($"{GetType().Name}: An error occurred while sending response to {Message.From}", e);
            }

            Status = true;
        }
    }
}
