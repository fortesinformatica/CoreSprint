using NetTelegramBotApi.Types;

namespace CoreSprint.Telegram.TelegramCommands
{
    public interface ITelegramCommand
    {
        string Name { get; }
        bool AllowParlallelExecution { get; }
        void Execute(Message message);
        void SendToChat(long chatId, string message);
    }
}