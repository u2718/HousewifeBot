using System;

namespace HousewifeBot
{
    class StartCommand : Command
    {
        public override bool Execute()
        {
            try
            {
                TelegramApi.SendMessage(Message.From, $"Шалом, {Message.From.FirstName}. Введи '/help', чтобы посмотреть список команд.");
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
    }
}
