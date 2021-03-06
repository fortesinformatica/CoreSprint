using System;
using CoreSprint.Extensions;
using NetTelegramBotApi;
using NetTelegramBotApi.Requests;
using NetTelegramBotApi.Types;

namespace CoreSprint.Telegram.TelegramCommands
{
    public abstract class TelegramCommand : ITelegramCommand
    {
        protected readonly TelegramBot TelegramBot;

        protected TelegramCommand(TelegramBot telegramBot)
        {
            TelegramBot = telegramBot;
        }

        public abstract string Name { get; }

        public virtual bool AllowParlallelExecution { get; } = true;

        public abstract void Execute(Message message);

        public void SendToChat(long chatId, string message)
        {
            SendMessageToChat(TelegramBot, chatId, message);
        }

        public static void SendMessageToChat(TelegramBot telegramBot, long chatId, string message)
        {
            var sendMessage = new SendMessage(chatId, message);

            Console.WriteLine("Enviando mensagem para o chat...\r\n{0}", sendMessage.Text);
            var result = telegramBot.MakeRequestAsync(sendMessage).Result;

            Console.WriteLine(result == null
                ? "Erro: N�o foi poss�vel enviar a mensagem para o chat!"
                : $"Mensagem enviada em {DateTime.Now.ToHumanReadable()}!");
        }
    }
}