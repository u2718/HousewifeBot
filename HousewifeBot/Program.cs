using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Telegram;

namespace HousewifeBot
{
    class Program
    {
        static void Main(string[] args)
        {
            var token = File.ReadAllText(@"token.txt");
            TelegramApi tg = new TelegramApi(token);
            var botUser = tg.GetMe();
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

            while (true)
            {
                foreach (var update in tg.Updates)
                {
                    Command command = null;
                    if (update.Value.Count == 0)
                    {
                        continue;
                    }
                    Message message = null;
                    update.Value.TryDequeue(out message);
                    try
                    {
                        command = Command.CreateCommand(message.Text);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        continue;
                    }
                    command.TelegramApi = tg;
                    command.Message = message;
                    try
                    {
                        command.Execute();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);                        
                    }
                }
                Thread.Sleep(200);
            }
        }
    }
}
