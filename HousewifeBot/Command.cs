using System;
using System.Text.RegularExpressions;
using Telegram;

namespace HousewifeBot
{
    public abstract class Command
    {
        private static readonly Regex ArgumentsRegex = new Regex(@"/\w+\s*(.+)?");

        private string _arguments;

        public string Arguments
        {
            get
            {
                if (!string.IsNullOrEmpty(_arguments)) return _arguments;

                try
                {
                    _arguments = ArgumentsRegex.Match(Message.Text).Groups[1].Value;
                }
                catch (Exception e)
                {
                    throw new Exception("Error while parsing command string", e);
                }
                return _arguments;
            }
        }

        public TelegramApi TelegramApi { get; set; }
        public Message Message { get; set; }

        public bool Status { get; protected set; } = false;

        protected Command(TelegramApi telegramApi, Message message)
        {
            TelegramApi = telegramApi;
            Message = message;
        }

        protected Command()
        { }

        abstract public void Execute();

        public static Command CreateCommand(string command)
        {
            Regex downloadCommandRegex = new Regex(string.Format(DownloadCommand.DownloadCommandFormat, @"(\d+)", @"(.+)"));
            if (downloadCommandRegex.IsMatch(command))
            {
                Match downloadMatch = downloadCommandRegex.Match(command);
                return new DownloadCommand(int.Parse(downloadMatch.Groups[1].Value), downloadMatch.Groups[2].Value);
            }
            Regex subscribeCommandRegex = new Regex(string.Format(SubscribeCommand.SubscribeCommandFormat, @"(\d+)"));
            if (subscribeCommandRegex.IsMatch(command))
            {
                Match subscribeMatch = subscribeCommandRegex.Match(command);
                return new SubscribeCommand(int.Parse(subscribeMatch.Groups[1].Value));
            }
            Regex unsubscribeCommandRegex = new Regex(string.Format(UnsubscribeCommand.UnsubscribeCommandFormat, @"(\d+)"));
            if (unsubscribeCommandRegex.IsMatch(command))
            {
                Match unsubscribeMatch = unsubscribeCommandRegex.Match(command);
                return new UnsubscribeCommand(int.Parse(unsubscribeMatch.Groups[1].Value));
            }

            switch (command.ToLower())
            {
                case @"/start":
                    return new StartCommand();
                case @"/help":
                    return new HelpCommand();                
                case @"/shows":
                    return new ShowsCommand();
                case @"/subscribe":
                    return new SubscribeCommand();
                case @"/subscribe_all":
                    return new SubscribeAllCommand();
                case @"/unsubscribe":
                    return new UnsubscribeCommand();
                case @"/unsubscribe_all":
                    return new UnsubscribeAllCommand();
                case @"/my_subscriptions":
                    return new MySubscriptionsCommand();
                case @"/settings":
                    return new SettingsCommand();
                default:
                    return new UnknownCommand();
            }
        }
    }
}
