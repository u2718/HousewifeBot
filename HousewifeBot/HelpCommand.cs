using System;

namespace HousewifeBot
{
    class HelpCommand : Command
    {
        public override bool Execute()
        {
            try
            {
                TelegramApi.SendMessage(Message.From, "Команды: \n" +
                "/serials - вывести список всех сериалов \n" +
                "/subscribe - подписаться на сериал \n" +
                "/subscribe_all - подписаться на ВСЕ сериалы!!! \n" +
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
