using System;

namespace HousewifeBot
{
    class UnknownCommand : Command
    {
        public override void Execute()
        {
            Program.Logger.Debug($"{GetType().Name}: Sending message to {Message.From}");
            try
            {
                TelegramApi.SendMessage(Message.From, "Пощади, братишка");
            }
            catch (Exception e)
            {
                throw new Exception($"{GetType().Name}: An error occurred while sending message to {Message.From}", e);
            }

            Status = true;
        }
    }
}
