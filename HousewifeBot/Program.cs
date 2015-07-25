using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Telegram;
using System.Configuration;
namespace HousewifeBot
{
    class Program
    {
        public static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        static void Main()
        {
            Logger.Info($"HousewifeBot started: {Assembly.GetEntryAssembly().Location}");

            string token;
            try
            {
                token = ConfigurationManager.AppSettings["TelegramToken"];
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "An error occurred while loading token");
                return;
            }

            TelegramApi tg = new TelegramApi(token);
            try
            {
                Logger.Debug("Executing GetMe");
                var botUser = tg.GetMe();
                Logger.Debug($"GetMe returned {botUser}");
            }
            catch (Exception e)
            {
                Logger.Error("GetMe failed");
                Logger.Error(e);
                return;
            }

            tg.StartPolling();

            Notifier notifier = new Notifier(tg);
            var updateNotificationsTask = new Task(
                () =>
                {
                    while (true)
                    {
                        notifier.UpdateNotifications();
                        Thread.Sleep(5000);
                    }
                }
                );
            updateNotificationsTask.Start();

            var sendNotificationsTask = new Task(
                () =>
                {
                    while (true)
                    {
                        notifier.SendNotifications();
                        Thread.Sleep(10000);
                    }
                }
                );
            sendNotificationsTask.Start();

            var processingCommandUsers = new ConcurrentDictionary<User, bool>();
            Regex commandRegex = new Regex(@"(/\w+)\s*");

            while (true)
            {
                foreach (var update in tg.Updates)
                {
                    if (processingCommandUsers.ContainsKey(update.Key) &&
                        processingCommandUsers[update.Key])
                    {
                        continue;
                    }

                    if (update.Value.Count == 0)
                    {
                        continue;
                    }
                    Message message;
                    update.Value.TryDequeue(out message);

                    Logger.Debug($"Received message '{message.Text}' from " +
                                 $"{message.From.FirstName} {message.From.LastName}");
                    string commandTitle = commandRegex.Match(message.Text).Groups[1].Value;

                    Logger.Debug($"Creating command object for '{commandTitle}'");
                    var command = Command.CreateCommand(commandTitle);
                    Logger.Info($"Received {command.GetType().Name} from " +
                                $"{message.From.FirstName} {message.From.LastName}");

                    command.TelegramApi = tg;
                    command.Message = message;

                    Logger.Debug($"Executing {command.GetType().Name}");
                    processingCommandUsers[update.Key] = true;
                    Task commandTask = Task.Run(() =>
                    {
                        try
                        {
                            command.Execute();
                        }
                        catch (Exception e)
                        {
                            Logger.Error($"An error occurred while executing {command.GetType().Name}.\n" +
                                         $"Message: {command.Message.Text}\n" +
                                         $"Arguments: {command.Arguments}\n" +
                                         $"User: {command.Message.From}");
                            Logger.Error(e);
                        }
                    }
                        );
                    commandTask.ContinueWith(task =>
                    {
                        processingCommandUsers[update.Key] = false;
                        Logger.Debug($"{command.GetType().Name} from " +
                                     $"{message.From.FirstName} {message.From.LastName} {(command.Status ? "succeeded" : "failed")}");
                    });
                }
                Thread.Sleep(200);
            }
        }
    }
}
