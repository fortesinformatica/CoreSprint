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
        private readonly ICommand _currentSprintUpdate;

        public TelegramCurrentSprintUpdate(TelegramBot telegramBot, ICoreSprintFactory coreSprintFactory, string trelloBoardId, string spreadsheetId)
            : base(telegramBot)
        {
            _currentSprintUpdate = coreSprintFactory.GetCurrentSprintUpdate(trelloBoardId, spreadsheetId);
        }

        public override string Name { get; } = "update_report";

        public override bool AllowParlallelExecution => false;

        public override void Execute(Message message)
        {
            SendToChat(message.Chat.Id, "Vou processar o quadro do trello e atualizar a planilha. Assim que terminar aviso.");
            _currentSprintUpdate.Execute();
            SendToChat(message.Chat.Id, $"Atualização da planilha do sprint concluída em {DateTime.Now.ToHumanReadable()}.\r\nResposta à \"{message.Text}\"");
        }
    }
}