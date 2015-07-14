using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HousewifeBot
{
    class UnknownCommand : Command
    {
        public override bool Execute()
        {
            try
            {
                TelegramApi.SendMessage(Message.From.Id, "Пощади, братишка");
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
    }
}
