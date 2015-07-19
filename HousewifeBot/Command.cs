using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram;

namespace HousewifeBot
{
    public abstract class Command
    {
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
                default:
                    return new UnknownCommand();
            }
        }
    }
}
