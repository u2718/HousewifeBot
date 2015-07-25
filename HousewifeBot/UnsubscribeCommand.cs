using System;
using System.Linq;
using DAL;

namespace HousewifeBot
{
    public class UnsubscribeCommand : Command
    {
        public override bool Execute()
        {
            string response;

            User user = null;
            bool userHasSubscriptions = false;
            using (var db = new AppDbContext())
            {
                Program.Logger.Debug(
                    $"{GetType().Name}: Searching user with TelegramId: {Message.From.Id} in database");
                try
                {
                    user = db.Users.FirstOrDefault(u => u.TelegramUserId == Message.From.Id);
                }
                catch (Exception e)
                {
                    throw new Exception($"{GetType().Name}: An error occurred while searching user in database", e);
                }

                if (user == null)
                {
                    Program.Logger.Debug(
                        $"{GetType().Name}: User {Message.From.FirstName} {Message.From.LastName} is not exists");
                }
                else
                {
                    Program.Logger.Debug($"{GetType().Name}: Checking if user has subscriptions");
                    try
                    {
                        userHasSubscriptions = db.Subscriptions.Any(s => s.User.Id == user.Id);
                    }
                    catch (Exception e)
                    {
                        throw new Exception(
                            $"{GetType().Name}: An error occurred while checking if user has subscriptions", e);
                    }
                }
            }

            if (userHasSubscriptions)
            {
                string serialTitle;
                if (string.IsNullOrEmpty(Arguments))
                {
                    Program.Logger.Debug($"{GetType().Name}: Sending 'Enter serial title' prompt");
                    try
                    {
                        TelegramApi.SendMessage(Message.From, "Введите название сериала");
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"{GetType().Name}: An error occurred while sending prompt", e);
                    }

                    Program.Logger.Debug($"{GetType().Name}: Waiting for a message that contains serial title");
                    try
                    {
                        serialTitle = TelegramApi.WaitForMessage(Message.From).Text;
                    }
                    catch (Exception e)
                    {
                        throw new Exception(
                            $"{GetType().Name}: An error occurred while waiting for a message that contains serial title",
                            e);
                    }
                }
                else
                {
                    serialTitle = Arguments;
                }

                Program.Logger.Info(
                    $"{GetType().Name}: {Message.From.FirstName} {Message.From.FirstName} is trying to unsubscribe from '{serialTitle}'");

                using (var db = new AppDbContext())
                {
                    do
                    {
                        Program.Logger.Debug($"{GetType().Name}: Searching serial {serialTitle} in database");

                        Show serial;
                        try
                        {
                            serial = db.Shows.FirstOrDefault(s =>
                                s.Title.ToLower() == serialTitle.ToLower() ||
                                s.OriginalTitle.ToLower() == serialTitle.ToLower()
                                );
                        }
                        catch (Exception e)
                        {
                            throw new Exception(
                                $"{GetType().Name}: An error occurred while searching serial {serialTitle} in database",
                                e);
                        }

                        if (serial == null)
                        {
                            Program.Logger.Info($"{GetType().Name}: Serial {serialTitle} was not found");
                            response = $"Сериал '{serialTitle}' не найден";
                            break;
                        }

                        Program.Logger.Debug($"{GetType().Name}: Checking for subscription existence");
                        Subscription subscription;
                        try
                        {
                            subscription = db.Subscriptions
                                .FirstOrDefault(s => s.User.Id == user.Id && s.Show.Id == serial.Id);
                        }
                        catch (Exception e)
                        {
                            throw new Exception(
                                $"{GetType().Name}: An error occurred while checking for subscription existence", e);
                        }

                        if (subscription == null)
                        {
                            Program.Logger.Debug(
                                $"{GetType().Name}: User {Message.From.FirstName} {Message.From.LastName} is not subscribed to {serial.OriginalTitle}");
                            response = $"Вы не подписаны на сериал '{serial.Title}'";
                            break;
                        }

                        Program.Logger.Debug($"{GetType().Name}: Deleting notifications for subscription");
                        try
                        {
                            db.Notifications.RemoveRange(
                                db.Notifications.Where(n => n.Subscription.Id == subscription.Id)
                                );
                        }
                        catch (Exception e)
                        {
                            throw new Exception(
                                $"{GetType().Name}: An error occurred while deleting notifications for subscription", e);
                        }

                        Program.Logger.Debug($"{GetType().Name}: Deleting subscription");
                        try
                        {
                            db.Subscriptions.Remove(subscription);
                        }
                        catch (Exception e)
                        {
                            throw new Exception($"{GetType().Name}: An error occurred while deleting subscription", e);
                        }

                        response = $"Вы отписались от сериала '{serial.Title}'";
                    } while (false);

                    try
                    {
                        db.SaveChanges();
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"{GetType().Name}: An error occurred while saving changes to database", e);
                    }
                }
            }
            else
            {
                response = "Вы не подписаны ни на один сериал";
            }

            Program.Logger.Debug($"{GetType().Name}: Sending response to {Message.From.FirstName} {Message.From.LastName}");
            try
            {
                TelegramApi.SendMessage(Message.From, response);
            }
            catch (Exception e)
            {
                throw new Exception($"{GetType().Name}d: An error occurred while sending response to {Message.From.FirstName} {Message.From.LastName}", e);
            }

            Status = true;
            return true;
        }
    }
}
