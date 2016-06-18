using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Data.Entity;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DAL;
using NLog;
using Telegram;
using User = Telegram.User;

namespace HousewifeBot
{
    internal class Program
    {
        public static readonly Logger Logger = LogManager.GetLogger("Common");
        private static readonly Regex CommandRegex = new Regex(@"(/\w+)\s*");

        private static string token;
        private static int updateNotificationsInterval;
        private static int sendNotificationsInterval;
        private static int sendShowNotificationsInterval;
        private static int retryPollingDelay;
        private static Task updateNotificationsTask;
        private static Task sendEpisodesNotificationsTask;
        private static Task sendShowsNotificationsTask;

        private static bool LoadSettings()
        {
            bool result = true;
            try
            {
                token = ConfigurationManager.AppSettings["TelegramToken"];
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "An error occurred while loading token");
                result = false;
            }

            try
            {
                updateNotificationsInterval = int.Parse(ConfigurationManager.AppSettings["UpdateNotificationsInterval"]);
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "An error occurred while loading update notifications interval");
                result = false;
            }

            try
            {
                sendNotificationsInterval = int.Parse(ConfigurationManager.AppSettings["SendNotificationsInterval"]);
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "An error occurred while loading send notifications interval");
                result = false;
            }

            try
            {
                sendShowNotificationsInterval = int.Parse(ConfigurationManager.AppSettings["SendShowNotificationsInterval"]);
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "An error occurred while loading send show notifications interval");
                result = false;
            }

            try
            {
                retryPollingDelay = int.Parse(ConfigurationManager.AppSettings["RetryPollingDelay"]);
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "An error occurred while loading retry polling delay");
                result = false;
            }

            return result;
        }

        private static void StartPolling(TelegramApi api)
        {
            Logger.Debug("Starting polling");
            Task pollingTask = api.StartPolling();

            pollingTask.ContinueWith(
                e =>
                {
                    Logger.Error(e.Exception, "An error occurred while retrieving updates");
                    Thread.Sleep(retryPollingDelay);
                    StartPolling(api);
                },
                TaskContinuationOptions.OnlyOnFaulted);
        }

        private static void Main()
        {
            Logger.Info($"HousewifeBot started: {Assembly.GetEntryAssembly().Location}");
            if (!LoadSettings())
            {
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

            Database.SetInitializer(new MigrateDatabaseToLatestVersion<AppDbContext, DAL.Migrations.Configuration>());
            Notifier notifier = new Notifier(tg);
            StartUpdateNotificationsTask(notifier);
            StartSendEpisodesNotificationsTask(notifier);
            StartSendShowsNotificationsTask(notifier);

            var processingCommandUsers = new ConcurrentDictionary<User, bool>();

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

                    Logger.Debug($"Received message '{message.Text}' from {message.From}");

                    var command = GetCommand(tg, message);
                    if (command == null)
                    {
                        continue;
                    }

                    Logger.Debug($"Executing {command.GetType().Name}");
                    processingCommandUsers[update.Key] = true;
                    var commandTask = StartCommandTask(command);
                    commandTask.ContinueWith(task =>
                    {
                        processingCommandUsers[update.Key] = false;
                        Logger.Debug($"{command.GetType().Name} from {message.From} {(command.Status ? "succeeded" : "failed")}");
                    });
                }

                Thread.Sleep(200);
            }
        }

        private static Task StartCommandTask(Command command)
        {
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
            });

            return commandTask;
        }

        private static Command GetCommand(TelegramApi tg, Message message)
        {
            string commandTitle;
            try
            {
                commandTitle = CommandRegex.Match(message.Text).Groups[1].Value;
            }
            catch (Exception e)
            {
                Logger.Error(e, "An error occurred while parsing command title");
                return null;
            }

            Logger.Debug($"Creating command object for '{message.Text}'");
            var command = Command.CreateCommand(commandTitle);
            Logger.Info($"Received {command.GetType().Name} from " +
                        $"{message.From}");

            command.TelegramApi = tg;
            command.Message = message;
            return command;
        }

        private static void StartSendShowsNotificationsTask(Notifier notifier)
        {
            sendShowsNotificationsTask = new Task(
                () =>
                {
                    while (true)
                    {
                        notifier.SendShowsNotifications();
                        Thread.Sleep(sendShowNotificationsInterval);
                    }
                });
            sendShowsNotificationsTask.Start();
        }

        private static void StartSendEpisodesNotificationsTask(Notifier notifier)
        {
            sendEpisodesNotificationsTask = new Task(
                () =>
                {
                    while (true)
                    {
                        notifier.SendEpisodesNotifications();
                        Thread.Sleep(sendNotificationsInterval);
                    }
                });

            sendEpisodesNotificationsTask.Start();
        }

        private static void StartUpdateNotificationsTask(Notifier notifier)
        {
            updateNotificationsTask = new Task(
                () =>
                {
                    while (true)
                    {
                        notifier.UpdateNotifications();
                        Thread.Sleep(updateNotificationsInterval);
                    }
                });

            updateNotificationsTask.Start();
        }
    }
}