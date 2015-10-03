using System;
using DAL;
using Telegram;

namespace HousewifeBot
{
    public class SettingsCommand : Command
    {
        private const string WebUiUrlSetCommand = "/webUi";
        private const string WebUiPasswordSetCommand = "/webUiPwd";
        private const string SiteLoginSetCommand = "/login";
        private const string SitePasswordSetCommand = "/pwd";
        private const string SaveSettingsCommand = "/save";
        private const string CancelCommand = "/cancel";

        public override void Execute()
        {
            using (AppDbContext db = new AppDbContext())
            {
                Settings settings = null;
                Program.Logger.Debug($"{GetType().Name}: Searching user's settings in database");
                try
                {
                    settings = db.GetSettingsByUser(db.GetUserByTelegramId(Message.From.Id));
                }
                catch (Exception e)
                {
                    Program.Logger.Error(e, $"{GetType().Name}: An error occurred while searching user's settings in database");
                }

                string text =
                    $"{(String.IsNullOrEmpty(settings?.WebUiUrl) ? "*" : String.Empty)}Адрес web-интерфейса uTorrent: {WebUiUrlSetCommand}\n" +
                    $"{(String.IsNullOrEmpty(settings?._WebUiPassword) ? "*" : String.Empty)}Пароль web-интерфейса uTorrent: {WebUiPasswordSetCommand}\n" +
                    $"{(String.IsNullOrEmpty(settings?.SiteLogin) ? "*" : String.Empty)}Имя учетной записи LostFilm.tv: {SiteLoginSetCommand}\n" +
                    $"{(String.IsNullOrEmpty(settings?._SitePassword) ? "*" : String.Empty)}Пароль учетной записи LostFilm.tv: {SitePasswordSetCommand}\n" +
                    $"Сохранить настройки: {SaveSettingsCommand}\n" +
                    $"Выйти без сохранения изменений: {CancelCommand}";

                Program.Logger.Debug($"{GetType().Name}: Sending commands list");
                try
                {
                    TelegramApi.SendMessage(Message.From, text);
                }
                catch (Exception e)
                {
                    throw new Exception($"{GetType().Name}: An error occurred while sending commands list", e);
                }

                bool newSettings = false;
                if (settings == null)
                {
                    Program.Logger.Debug($"{GetType().Name}: Creating new settings for {Message.From}");
                    newSettings = true;
                    settings = new Settings {User = db.GetUserByTelegramId(Message.From.Id)};
                }

                Message msg;
                do
                {
                    Program.Logger.Debug($"{GetType().Name}: Waiting for a command");
                    try
                    {
                        msg = TelegramApi.WaitForMessage(Message.From);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"{GetType().Name}: An error occurred while waiting for a command", e);
                    }

                    switch (msg.Text)
                    {
                        case WebUiUrlSetCommand:
                            Program.Logger.Debug($"{GetType().Name}: {WebUiUrlSetCommand} command received");
                            settings.WebUiUrl = TelegramApi.WaitForMessage(Message.From).Text;
                            break;
                        case WebUiPasswordSetCommand:
                            Program.Logger.Debug($"{GetType().Name}: {WebUiPasswordSetCommand} command received");
                            settings.WebUiPassword = TelegramApi.WaitForMessage(Message.From).Text;
                            break;
                        case SiteLoginSetCommand:
                            Program.Logger.Debug($"{GetType().Name}: {SiteLoginSetCommand} command received");
                            settings.SiteLogin = TelegramApi.WaitForMessage(Message.From).Text;
                            break;
                        case SitePasswordSetCommand:
                            Program.Logger.Debug($"{GetType().Name}: {SitePasswordSetCommand} command received");
                            settings.SitePassword = TelegramApi.WaitForMessage(Message.From).Text;
                            break;
                        case SaveSettingsCommand:
                            Program.Logger.Debug($"{GetType().Name}: {SaveSettingsCommand} command received");
                            break;
                        case CancelCommand:
                            Program.Logger.Debug($"{GetType().Name}: {CancelCommand} command received");
                            break;
                        default:
                            Program.Logger.Debug($"{GetType().Name}: Unknown command received");
                            TelegramApi.SendMessage(Message.From, "Пощади, братишка");
                            break;
                    }
                } while (msg.Text != SaveSettingsCommand && msg.Text != CancelCommand);

                if (msg.Text == SaveSettingsCommand)
                {
                    Program.Logger.Debug($"{GetType().Name}: Saving changes to database");
                    try
                    {
                        if (newSettings)
                        {
                            db.Settings.Add(settings);
                        }
                        db.SaveChanges();
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"{GetType().Name}: An error occurred while saving changes to database", e);
                    }
                }
                else
                {
                    Program.Logger.Debug($"{GetType().Name}: Exiting without saving changes to database");
                }
            }
            Status = true;
        }
    }
}