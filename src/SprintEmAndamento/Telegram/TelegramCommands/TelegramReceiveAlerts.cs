using System.Collections.Generic;
using System.IO;
using System.Linq;
using NetTelegramBotApi;
using NetTelegramBotApi.Types;

namespace CoreSprint.Telegram.TelegramCommands
{
    public class TelegramReceiveAlerts : TelegramCommand
    {
        public TelegramReceiveAlerts(TelegramBot telegramBot) : base(telegramBot)
        {
        }

        public override string Name { get; } = "receive_alerts";

        public override void Execute(Message message)
        {
            var chatId = message.Chat.Id.ToString();
            var chats = File.Exists(CoreSprintApp.TelegramChatsPath)
                ? File.ReadAllLines(CoreSprintApp.TelegramChatsPath).ToList()
                : new List<string>();

            if (!chats.Contains(chatId))
                chats.Add(chatId);

            File.WriteAllLines(CoreSprintApp.TelegramChatsPath, chats);

            SendToChat(message.Chat.Id, "Este chat foi registrado para receber alertas!");
        }
    }
}