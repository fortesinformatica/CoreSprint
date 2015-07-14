using System;
using CoreSprint.Extensions;
using CoreSprint.Factory;
using NetTelegramBotApi;
using NetTelegramBotApi.Types;

namespace CoreSprint.Integration.TelegramCommands
{
    public class TelegramCurrentSprintUpdate : TelegramCommand
    {
        private readonly CurrentSprintUpdate _currentSprintUpdate;

        public TelegramCurrentSprintUpdate(TelegramBot telegramBot, ICoreSprintFactory coreSprintFactory, string trelloBoardId, string spreadsheetId) : base(telegramBot)
        {
            _currentSprintUpdate = new CurrentSprintUpdate(coreSprintFactory, trelloBoardId, spreadsheetId);
        }

        public override void Execute(Message message)
        {
            SendToChat(message.Chat.Id, "Processando quadro do trello e atualizando planilha do sprint...");
            _currentSprintUpdate.Execute();
            SendToChat(message.Chat.Id, string.Format("Atualização da planilha do sprint concluída em {0}.", DateTime.Now.ToHumanReadable()));
        }
    }
}