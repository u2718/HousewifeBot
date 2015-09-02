using System;
using System.Linq;
using DAL;

namespace HousewifeBot
{
    public class UnsubscribeAllCommand : Command
    {
        public override bool Execute()
        {
            string response;
            using (var db = new AppDbContext())
            {
                do
                {
                    User user;
                    Program.Logger.Debug(
                    $"{GetType().Name}: Searching user with TelegramId: {Message.From.Id} in database");
                    try
                    {
                        user = db.GetUserByTelegramId(Message.From.Id);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"{GetType().Name}: An error occurred while searching user in database", e);
                    }

                    if (user == null)
                    {
                        Program.Logger.Debug($"{GetType().Name}: User with TelegramId: {Message.From.Id} is not found");
                        response = "Вы не подписаны ни на один сериал";
                        break;
                    }


                    IQueryable<Subscription> subscriptions;
                    Program.Logger.Debug($"{GetType().Name}: Retrieving subscriptions of {user}");
                    try
                    {
                        subscriptions = db.Subscriptions.Where(s => s.User.Id == user.Id);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"{GetType().Name}: An error occurred while retrieving subscriptions of {user}", e);
                    }

                    if (!subscriptions.Any())
                    {
                        Program.Logger.Debug($"{GetType().Name}: {user} has no subscriptions");
                        response = "Вы не подписаны ни на один сериал";
                        break;
                    }

                    Program.Logger.Debug($"{GetType().Name}: Deleting notifications for all subscriptions");
                    try
                    {
                        db.Notifications.RemoveRange(
                            db.Notifications.Where(n => subscriptions.Any(s => s.Id == n.Subscription.Id))
                            );
                    }
                    catch (Exception e)
                    {
                        throw new Exception(
                                $"{GetType().Name}: An error occurred while deleting notifications for all subscription", e);
                    }

                    Program.Logger.Debug($"{GetType().Name}: Deleting all subscriptions");
                    try
                    {
                        db.Subscriptions.RemoveRange(subscriptions);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"{GetType().Name}: An error occurred while deleting all subscriptions", e);
                    }

                    response = "Вы отписались от всех сериалов";
                } while (false);

                Program.Logger.Debug($"{GetType().Name}: Saving changes to database");
                try
                {
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    throw new Exception($"{GetType().Name}: An error occurred while saving changes to database", e);
                }
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
            return true;
        }
    }
}
