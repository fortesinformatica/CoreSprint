using NetTelegramBotApi.Types;

namespace CoreSprint.Integration.TelegramCommands
{
    public interface ITelegramCommand
    {
        void Execute(Message message);
        void SendToChat(long chatId, string message);
    }
}