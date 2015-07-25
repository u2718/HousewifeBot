using System;
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
                    User user;
                    Program.Logger.Debug(
                    $"{GetType().Name}: Searching user with TelegramId: {Message.From.Id} in data base");
                    try
                    {
                        user = db.Users.FirstOrDefault(u => u.TelegramUserId == Message.From.Id);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"{GetType().Name}: An error occurred while searching user in data base", e);
                    }

                    if (user == null)
                    {
                        Program.Logger.Debug($"{GetType().Name}: User with TelegramId: {Message.From.Id} is not found");
                        response = "Вы не подписаны ни на один сериал";
                        break;
                    }


                    IQueryable<Subscription> subscriptions = null;
                    Program.Logger.Debug($"{GetType().Name}: Retrieving subscriptions of {user.FirstName} {user.LastName}");
                    try
                    {
                        subscriptions = db.Subscriptions.Where(s => s.User.Id == user.Id);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"{GetType().Name}: An error occurred while retrieving subscriptions of {user.FirstName} {user.LastName}", e);
                    }

                    if (!subscriptions.Any())
                    {
                        Program.Logger.Debug($"{GetType().Name}: {user.FirstName} {user.LastName} has no subscriptions");
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

                Program.Logger.Debug($"{GetType().Name}: Saving changes to data base");
                try
                {
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    throw new Exception($"{GetType().Name}: An error occurred while saving changes to data base", e);
                }
            }

            Program.Logger.Debug($"{GetType().Name}: Sending response to {Message.From.FirstName} {Message.From.LastName}");
            try
            {
                TelegramApi.SendMessage(Message.From, response);
            }
            catch (Exception e)
            {
                throw new Exception($"{GetType().Name}: An error occurred while sending response to {Message.From.FirstName} {Message.From.LastName}", e);
            }

            Status = true;
            return true;
        }
    }
}
