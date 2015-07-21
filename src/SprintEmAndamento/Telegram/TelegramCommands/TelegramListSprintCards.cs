using System;
using CoreSprint.Extensions;
using CoreSprint.Factory;
using CoreSprint.Integration;
using NetTelegramBotApi;
using NetTelegramBotApi.Types;

namespace CoreSprint.Telegram.TelegramCommands
{
    public class TelegramListSprintCards : TelegramCommand
    {
        private readonly ListSprintCards _listSprintCards;

        public TelegramListSprintCards(TelegramBot telegramBot, ICoreSprintFactory coreSprintFactory, string trelloBoardId, string spreadsheetId)
            : base(telegramBot)
        {
            _listSprintCards = new ListSprintCards(coreSprintFactory, trelloBoardId, spreadsheetId); //TODO: criar m�todo no factory
        }

        public override void Execute(Message message)
        {
            SendToChat(message.Chat.Id, "Vou recuperar os cart�es do quadro do trello e atualizar a planilha. Assim que terminar aviso.");
            _listSprintCards.Execute();
            SendToChat(message.Chat.Id,
                string.Format(
                    "Atualiza��o da lista de cart�es do sprint na planilha conclu�da em {0}.\r\nResposta � \"{1}\"",
                    DateTime.Now.ToHumanReadable(), message.Text));
        }
    }
}