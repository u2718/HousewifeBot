using System;

namespace HousewifeBot
{
    class StartCommand : Command
    {
        public override bool Execute()
        {
            try
            {
                TelegramApi.SendMessage(Message.From, $"Шалом, {Message.From.FirstName}. \n"+ 
                "Список команд: \n" +
                "/serials - вывести список всех сериалов \n" +
                "/subscribe - подписаться на сериал \n" +
                "/unsubscribe - отписаться от сериала \n" +
                "/unsubscribe_all - отписаться от всех сериалов \n" +
                "/help - справка"
                );
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
    }
}
