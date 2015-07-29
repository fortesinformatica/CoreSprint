using System.Collections.Generic;
using NetTelegramBotApi.Types;

namespace CoreSprint.Telegram.TelegramCommands
{
    public interface ITelegramProactiveCommand
    {
        void Execute(IEnumerable<long> chats);
    }

    public interface ITelegramCommand
    {
        string Name { get; }
        bool AllowParlallelExecution { get; }
        void Execute(Message message);
        void SendToChat(long chatId, string message);
    }
}