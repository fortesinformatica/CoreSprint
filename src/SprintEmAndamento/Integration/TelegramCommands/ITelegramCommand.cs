using NetTelegramBotApi.Types;

namespace CoreSprint.Integration.TelegramCommands
{
    public interface ITelegramCommand
    {
        void Execute(Message message);
    }
}