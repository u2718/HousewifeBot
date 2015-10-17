using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Telegram;
using System.Configuration;
using System.Data.Entity;
using DAL;
using User = Telegram.User;

namespace HousewifeBot
{
    class Program
    {
        public static readonly Logger Logger = LogManager.GetLogger("Common");

        private static string _token;
        private static int _updateNotificationsInterval;
        private static int _sendNotificationsInterval;
        private static int _sendShowNotificationsInterval;
        private static int _retryPollingDelay;

        static bool LoadSettings()
        {
            bool result = true;
            try
            {
                _token = ConfigurationManager.AppSettings["TelegramToken"];
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "An error occurred while loading token");
                result = false;
            }

            try
            {
                _updateNotificationsInterval = int.Parse(ConfigurationManager.AppSettings["UpdateNotificationsInterval"]);
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "An error occurred while loading update notifications interval");
                result = false;
            }

            try
            {
                _sendNotificationsInterval = int.Parse(ConfigurationManager.AppSettings["SendNotificationsInterval"]);
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "An error occurred while loading send notifications interval");
                result = false;
            }

            try
            {
                _sendShowNotificationsInterval = int.Parse(ConfigurationManager.AppSettings["SendShowNotificationsInterval"]);
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "An error occurred while loading send show notifications interval");
                result = false;
            }

            try
            {
                _retryPollingDelay = int.Parse(ConfigurationManager.AppSettings["RetryPollingDelay"]);
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "An error occurred while loading retry polling delay");
                result = false;
            }

            return result;
        }

        private static void StartPolling(TelegramApi tgApi)
        {
            Logger.Debug("Starting polling");
            Task pollingTask = tgApi.StartPolling();

            pollingTask.ContinueWith(e =>
            {
                Logger.Error(e.Exception, "An error occurred while retrieving updates");
                Thread.Sleep(_retryPollingDelay);
                StartPolling(tgApi);
            },
          TaskContinuationOptions.OnlyOnFaulted); 
        }

        static void Main()
        {
            Logger.Info($"HousewifeBot started: {Assembly.GetEntryAssembly().Location}");
            if (!LoadSettings())
            {
                return;
            }   
            
            TelegramApi tg = new TelegramApi(_token);
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

            Database.SetInitializer(new MigrateDatabaseToLatestVersion<AppDbContext, DAL.Migrations.Configuration>());
            Notifier notifier = new Notifier(tg);
            var updateNotificationsTask = new Task(
                () =>
                {
                    while (true)
                    {
                        notifier.UpdateNotifications();
                        Thread.Sleep(_updateNotificationsInterval);
                    }
                }
                );
            updateNotificationsTask.Start();

            var sendEpisodesNotificationsTask = new Task(
                () =>
                {
                    while (true)
                    {
                        notifier.SendEpisodesNotifications();
                        Thread.Sleep(_sendNotificationsInterval);
                    }
                }
                );

            var sendShowsNotificationsTask = new Task(
                () =>
                {
                    while (true)
                    {
                        notifier.SendShowsNotifications();
                        Thread.Sleep(_sendShowNotificationsInterval);
                    }
                });

            sendEpisodesNotificationsTask.Start();
            sendShowsNotificationsTask.Start();

            var processingCommandUsers = new ConcurrentDictionary<User, bool>();
            Regex commandRegex = new Regex(@"(/\w+)\s*");

            StartPolling(tg);
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
                                 $"{message.From}");

                    string commandTitle;
                    try
                    {
                        commandTitle = commandRegex.Match(message.Text).Groups[1].Value;
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "An error occurred while parsing command title");
                        continue;
                    }

                    Logger.Debug($"Creating command object for '{message.Text}'");
                    var command = Command.CreateCommand(commandTitle);
                    Logger.Info($"Received {command.GetType().Name} from " +
                                $"{message.From}");

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
                                     $"{message.From} {(command.Status ? "succeeded" : "failed")}");
                    });
                }
                Thread.Sleep(200);
            }
        }
    }
}
