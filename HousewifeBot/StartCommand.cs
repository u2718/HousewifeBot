﻿using System;

namespace HousewifeBot
{
    public class StartCommand : Command
    {
        private const string HelpTemplate = "Шалом, {0}. \n" +
                                            "Список команд: \n" +
                                            "/shows - вывести список всех сериалов \n" +
                                            "/subscribe - подписаться на сериал \n" +
                                            "/unsubscribe - отписаться от сериала \n" +
                                            "/unsubscribe_all - отписаться от всех сериалов \n" +
                                            "/help - справка";

        public override void Execute()
        {
            Program.Logger.Debug($"{GetType().Name}: Sending help message to {Message.From}");
            try
            {
                TelegramApi.SendMessage(Message.From, string.Format(HelpTemplate, Message.From.FirstName));
            }
            catch (Exception e)
            {
                throw new Exception($"{GetType().Name}: An error occurred while sending help message to {Message.From}", e);
            }

            Status = true;
        }
    }
}
