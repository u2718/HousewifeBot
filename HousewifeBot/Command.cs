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

        abstract public bool Execute();

        public static Command CreateCommand(string command)
        {
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
                default:
                    return new UnknownCommand();
            }
        }
    }
}
