using System;

namespace HousewifeBot
{
    class UnknownCommand : Command
    {
        public override bool Execute()
        {
            try
            {
                TelegramApi.SendMessage(Message.From, "Пощади, братишка");
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
    }
}
