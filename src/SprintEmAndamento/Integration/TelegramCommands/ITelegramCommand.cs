namespace CoreSprint.Integration.TelegramCommands
{
    public interface ITelegramCommand
    {
        void Execute(long chatId);
    }
}