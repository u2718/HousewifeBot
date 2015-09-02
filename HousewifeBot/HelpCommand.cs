using System;

namespace HousewifeBot
{
    class HelpCommand : Command
    {
        public override bool Execute()
        {
            Program.Logger.Debug($"{GetType().Name}: Sending help message to {Message.From}");
            try
            {
                TelegramApi.SendMessage(Message.From, "Список команд: \n" +
                "/shows - вывести список всех сериалов \n" +
                "/subscribe - подписаться на сериал \n" +
                "/unsubscribe - отписаться от сериала \n" +
                "/unsubscribe_all - отписаться от всех сериалов \n" +
                "/help - справка"
                );
            }
            catch (Exception e)
            {
                throw new Exception($"{GetType().Name}: An error occurred while sending help messge to {Message.From}", e);
            }

            Status = true;
            return true;
        }
    }
}
