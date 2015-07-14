using System;
using CoreSprint.Extensions;
using NetTelegramBotApi;
using NetTelegramBotApi.Requests;
using NetTelegramBotApi.Types;

namespace CoreSprint.Integration.TelegramCommands
{
    public abstract class TelegramCommand : ITelegramCommand
    {
        protected readonly TelegramBot TelegramBot;

        protected TelegramCommand(TelegramBot telegramBot)
        {
            TelegramBot = telegramBot;
        }

        public abstract void Execute(Message message);

        public void SendToChat(long chatId, string message)
        {
            var sendMessage = new SendMessage(chatId, message);

            Console.WriteLine("Enviando mensagem para o chat...");
            var result = TelegramBot.MakeRequestAsync(sendMessage).Result;

            Console.WriteLine(result == null
                ? "Erro: N�o foi poss�vel enviar a mensagem para o chat!"
                : string.Format("Mensagem enviada em {0}!", DateTime.Now.ToHumanReadable()));
        }
    }
}