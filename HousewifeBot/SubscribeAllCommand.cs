using System;
using System.Collections.Generic;
using System.Linq;
using DAL;

namespace HousewifeBot
{
    public class SubscribeAllCommand : Command
    {
        public override void Execute()
        {
            using (AppDbContext db = new AppDbContext())
            {
                Program.Logger.Debug($"{GetType().Name}: Searching user with TelegramId: {Message.From.Id} in database");

                User user;
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
                    user = new User
                    {
                        TelegramUserId = Message.From.Id,
                        FirstName = Message.From.FirstName,
                        LastName = Message.From.LastName,
                        Username = Message.From.Username
                    };

                    Program.Logger.Info($"{GetType().Name}: {user} is new User");
                    Program.Logger.Debug($"{GetType().Name}: Adding user {user} to database");
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
                    Program.Logger.Debug($"{GetType().Name}: User {user} is already exist");
                }

                List<Show> shows;
                Program.Logger.Debug($"{GetType().Name}: Retrieving shows list");
                try
                {
                    shows = db.Shows.ToList();
                }
                catch (Exception e)
                {
                    throw new Exception($"{GetType().Name}: An error occurred while retrieving shows list", e);
                }

                List<Subscription> subscriptions;
                Program.Logger.Debug($"{GetType().Name}: Retrieving subscriptions of {user}");
                try
                {
                    subscriptions = db.Subscriptions.Where(s => s.User.Id == user.Id).ToList();
                }
                catch (Exception e)
                {
                    throw new Exception($"{GetType().Name}: An error occurred while retrieving subscriptions", e);
                }

                Program.Logger.Debug($"{GetType().Name}: Subscribing {user} to all shows");
                foreach (Show show in shows)
                {
                    Subscription subscription = new Subscription
                    {
                        User = user,
                        Show = show,
                        SubscriptionDate = DateTime.Now
                    };

                    if (subscriptions.Any(s => Equals(s.Show, show))) continue;

                    try
                    {
                        db.Subscriptions.Add(subscription);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"{GetType().Name}: An error occurred while subscribing {user} to {show.OriginalTitle}", e);
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
            Program.Logger.Debug($"{GetType().Name}: Sending response to {Message.From}");
            try
            {
                TelegramApi.SendMessage(Message.From, "Вы, братишка, подписаны на все сериалы");
            }
            catch (Exception e)
            {
                throw new Exception($"{GetType().Name}: An error occurred while sending response to {Message.From}", e);
            }

            Status = true;
        }
    }
}
