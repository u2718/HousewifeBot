using System;
using System.Collections.Generic;
using System.Linq;
using DAL;

namespace HousewifeBot
{
    public class SubscribeAllCommand : Command
    {
        public override bool Execute()
        {
            using (AppDbContext db = new AppDbContext())
            {
                Program.Logger.Debug($"{GetType().Name}: Searching user with TelegramId: {Message.From.Id} in database");

                User user;
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
                    user = new User
                    {
                        TelegramUserId = Message.From.Id,
                        FirstName = Message.From.FirstName,
                        LastName = Message.From.LastName,
                        Username = Message.From.Username
                    };

                    Program.Logger.Info($"{GetType().Name}: {user.FirstName} {user.LastName} is new User");

                    Program.Logger.Debug($"{GetType().Name}: Adding user {user.FirstName} {user.LastName} to database");
                    try
                    {
                        db.Users.Add(user);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"{GetType().Name}: An error occurred while adding user to database", e);
                    }
                }
                else
                {
                    Program.Logger.Debug($"{GetType().Name}: User {user.FirstName} {user.LastName} is already exist");
                }

                List<Show> shows;
                Program.Logger.Debug($"{GetType().Name}: Retrieving serials list");
                try
                {
                    shows = db.Shows.ToList();
                }
                catch (Exception e)
                {
                    throw new Exception($"{GetType().Name}: An error occurred while retrieving serials list", e);
                }

                List<Subscription> subscriptions;
                Program.Logger.Debug($"{GetType().Name}: Retrieving subscriptions of {user.FirstName} {user.LastName}");
                try
                {
                    subscriptions = db.Subscriptions.Where(s => s.User.Id == user.Id).ToList();
                }
                catch (Exception e)
                {
                    throw new Exception($"{GetType().Name}: An error occurred while retrieving subscriptions", e);
                }

                Program.Logger.Debug($"{GetType().Name}: Subscribing {user.FirstName} {user.LastName} to all serials");
                foreach (Show show in shows)
                {
                    Subscription subscription = new Subscription
                    {
                        User = user,
                        Show = show,
                        SubscriptionDate = DateTime.Now
                    };

                    if (!subscriptions.Any(s => Equals(s.Show, show)))
                    {
                        try
                        {
                            db.Subscriptions.Add(subscription);
                        }
                        catch (Exception e)
                        {
                            throw new Exception($"{GetType().Name}: An error occurred while subscribing {user.FirstName} {user.LastName} to {show.OriginalTitle}", e);
                        }
                    }
                }

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
            Program.Logger.Debug($"{GetType().Name}: Sending response to {Message.From.FirstName} {Message.From.LastName}");
            try
            {
                TelegramApi.SendMessage(Message.From, "Вы, братишка, подписаны на все сериалы");
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
