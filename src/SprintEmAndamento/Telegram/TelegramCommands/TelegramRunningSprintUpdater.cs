using System;
using CoreSprint.Extensions;
using CoreSprint.Factory;
using CoreSprint.Integration;
using NetTelegramBotApi;
using NetTelegramBotApi.Types;

namespace CoreSprint.Telegram.TelegramCommands
{
    public class TelegramRunningSprintUpdater : TelegramCommand
    {
        private readonly ICommand _runningSprintUpdate;

        public TelegramRunningSprintUpdater(TelegramBot telegramBot, ICoreSprintFactory coreSprintFactory, string trelloBoardId, string spreadsheetId)
            : base(telegramBot)
        {
            _runningSprintUpdate = coreSprintFactory.GetRunningSprintUpdater(trelloBoardId, spreadsheetId);
        }

        public override string Name { get; } = "update_report";

        public override bool AllowParlallelExecution => false;

        public override void Execute(Message message)
        {
            SendToChat(message.Chat.Id, "Vou processar o quadro do trello e atualizar a planilha. Assim que terminar aviso.");
            _runningSprintUpdate.Execute();
            SendToChat(message.Chat.Id, $"Atualização da planilha do sprint concluída em {DateTime.Now.ToHumanReadable()}.\r\nResposta à \"{message.Text}\"");
        }
    }
}