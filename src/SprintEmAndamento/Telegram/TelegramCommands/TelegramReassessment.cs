using System;
using System.Collections.Generic;
using System.Linq;
using CoreSprint.Factory;
using CoreSprint.Helpers;
using CoreSprint.Trello;
using NetTelegramBotApi;
using NetTelegramBotApi.Types;

namespace CoreSprint.Telegram.TelegramCommands
{
    public class TelegramReassessment : TelegramCommand, ITelegramProactiveCommand
    {
        private readonly string _trelloBoardId;
        private readonly ITrelloFacade _trelloFacade;
        private readonly ICardHelper _cardHelper;

        public TelegramReassessment(TelegramBot telegramBot, ICoreSprintFactory coreSprintFactory, string trelloBoardId) : base(telegramBot)
        {
            _trelloBoardId = trelloBoardId;
            _trelloFacade = coreSprintFactory.GetTrelloFacade();
            _cardHelper = coreSprintFactory.GetCardHelper();
        }

        public override string Name { get; } = "reassessment";

        public override void Execute(Message message)
        {
            var cards = _trelloFacade.GetCards(_trelloBoardId);

            cards.AsParallel().ForAll(card =>
            {
                var workedAndPending = _cardHelper.GetWorkedAndPending(card);
            });
        }

        public void Execute(IEnumerable<long> chats, string message = "")
        {
            throw new NotImplementedException();
        }
    }
}