using NetTelegramBotApi.Types;

namespace CoreSprint.Telegram.TelegramCommands
{
    public interface ITelegramCommand
    {
        void Execute(Message message);
        void SendToChat(long chatId, string message);
    }
}