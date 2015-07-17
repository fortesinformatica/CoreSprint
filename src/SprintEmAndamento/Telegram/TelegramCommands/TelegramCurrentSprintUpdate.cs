using System;
using CoreSprint.Extensions;
using CoreSprint.Factory;
using CoreSprint.Integration;
using NetTelegramBotApi;
using NetTelegramBotApi.Types;

namespace CoreSprint.Telegram.TelegramCommands
{
    public class TelegramCurrentSprintUpdate : TelegramCommand
    {
        private readonly CurrentSprintUpdate _currentSprintUpdate;

        public TelegramCurrentSprintUpdate(TelegramBot telegramBot, ICoreSprintFactory coreSprintFactory, string trelloBoardId, string spreadsheetId)
            : base(telegramBot)
        {
            _currentSprintUpdate = new CurrentSprintUpdate(coreSprintFactory, trelloBoardId, spreadsheetId); //TODO: criar m�todo no factory
        }

        public override void Execute(Message message)
        {
            SendToChat(message.Chat.Id, "Vou processar o quadro do trello e atualizar a planilha. Assim que terminar aviso.");
            _currentSprintUpdate.Execute();
            SendToChat(message.Chat.Id, string.Format("Atualiza��o da planilha do sprint conclu�da em {0}.", DateTime.Now.ToHumanReadable()));
        }
    }

    public class TelegramWorkExtractUpdate : TelegramCommand
    {
        private readonly WorkExtract _workExtract;

        public TelegramWorkExtractUpdate(TelegramBot telegramBot, ICoreSprintFactory coreSprintFactory, string trelloBoardId, string spreadsheetId)
            : base(telegramBot)
        {
            _workExtract = new WorkExtract(coreSprintFactory, trelloBoardId, spreadsheetId);
        }

        public override void Execute(Message message)
        {
            SendToChat(message.Chat.Id, "Vou processar o quadro do trello e atualizar a planilha. Assim que terminar aviso.");
            _workExtract.Execute();
            SendToChat(message.Chat.Id, string.Format("Atualiza��o conclu�da em {0} para a planilha de horas trabalhadas.", DateTime.Now.ToHumanReadable()));
        }
    }
}