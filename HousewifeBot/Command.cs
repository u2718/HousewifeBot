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
                if (string.IsNullOrEmpty(_arguments))
                {
                    _arguments = ArgumentsRegex.Match(Message.Text).Groups[1].Value;
                }
                return _arguments;
            }
        }

        public TelegramApi TelegramApi { get; set; }
        public Message Message { get; set; }

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
                case @"/serials":
                    return new SerialsCommand();
                case @"/subscribe":
                    return new SubscribeCommand();
                case @"/subscribe_all":
                    return new SubscribeAllCommand();
                case @"/unsubscribe":
                    return new UnsubscribeCommand();
                case @"/unsubscribe_all":
                    return new UnsubscribeAllCommand();
                default:
                    return new UnknownCommand();
            }
        }
    }
}
