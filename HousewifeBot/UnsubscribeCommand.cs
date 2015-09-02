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

            User user;
            bool userHasSubscriptions = false;
            using (var db = new AppDbContext())
            {
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
                    Program.Logger.Debug($"{GetType().Name}: User {Message.From} is not exists");
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
                        throw new Exception($"{GetType().Name}: An error occurred while checking if user has subscriptions", e);
                    }
                }
            }

            if (userHasSubscriptions)
            {
                string showTitle;
                if (string.IsNullOrEmpty(Arguments))
                {
                    Program.Logger.Debug($"{GetType().Name}: Sending 'Enter show title' prompt");
                    try
                    {
                        TelegramApi.SendMessage(Message.From, "Введите название сериала");
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"{GetType().Name}: An error occurred while sending prompt", e);
                    }

                    Program.Logger.Debug($"{GetType().Name}: Waiting for a message that contains show title");
                    try
                    {
                        showTitle = TelegramApi.WaitForMessage(Message.From).Text;
                    }
                    catch (Exception e)
                    {
                        throw new Exception(
                            $"{GetType().Name}: An error occurred while waiting for a message that contains show title",
                            e);
                    }
                }
                else
                {
                    showTitle = Arguments;
                }

                Program.Logger.Info(
                    $"{GetType().Name}: {Message.From} is trying to unsubscribe from '{showTitle}'");

                using (var db = new AppDbContext())
                {
                    do
                    {
                        Program.Logger.Debug($"{GetType().Name}: Searching show {showTitle} in database");

                        Show show;
                        try
                        {
                            show = db.GetShowByTitle(showTitle);
                        }
                        catch (Exception e)
                        {
                            throw new Exception(
                                $"{GetType().Name}: An error occurred while searching show {showTitle} in database",
                                e);
                        }

                        if (show == null)
                        {
                            Program.Logger.Info($"{GetType().Name}: Show {showTitle} was not found");
                            response = $"Сериал '{showTitle}' не найден";
                            break;
                        }

                        Program.Logger.Debug($"{GetType().Name}: Checking for subscription existence");
                        Subscription subscription;
                        try
                        {
                            subscription = db.Subscriptions
                                .FirstOrDefault(s => s.User.Id == user.Id && s.Show.Id == show.Id);
                        }
                        catch (Exception e)
                        {
                            throw new Exception(
                                $"{GetType().Name}: An error occurred while checking for subscription existence", e);
                        }

                        if (subscription == null)
                        {
                            Program.Logger.Debug(
                                $"{GetType().Name}: User {Message.From} is not subscribed to {show.OriginalTitle}");
                            response = $"Вы не подписаны на сериал '{show.Title}'";
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

                        response = $"Вы отписались от сериала '{show.Title}'";
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

            Program.Logger.Debug($"{GetType().Name}: Sending response to {Message.From}");
            try
            {
                TelegramApi.SendMessage(Message.From, response);
            }
            catch (Exception e)
            {
                throw new Exception($"{GetType().Name}d: An error occurred while sending response to {Message.From}", e);
            }

            Status = true;
            return true;
        }
    }
}
