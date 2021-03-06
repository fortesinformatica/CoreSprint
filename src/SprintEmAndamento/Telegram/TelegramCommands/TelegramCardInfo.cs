using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CoreSprint.Factory;
using CoreSprint.Helpers;
using CoreSprint.Spreadsheet;
using CoreSprint.Trello;
using NetTelegramBotApi;
using NetTelegramBotApi.Types;
using TrelloNet;

namespace CoreSprint.Telegram.TelegramCommands
{
    public class TelegramCardInfo : TelegramCommand
    {
        private readonly string _trelloBoardId;
        private readonly string _spreadsheetId;
        private readonly ITrelloFacade _trelloFacade;
        private readonly ICardHelper _cardHelper;
        private readonly ISpreadsheetFacade _spreadsheetFacade;
        private readonly ISprintRunningHelper _sprintRunningHelper;
        private readonly ITelegramHelper _telegramHelper;

        public TelegramCardInfo(TelegramBot telegramBot, ICoreSprintFactory coreSprintFactory, string trelloBoardId, string spreadsheetId)
            : base(telegramBot)
        {
            _trelloBoardId = trelloBoardId;
            _spreadsheetId = spreadsheetId;
            _trelloFacade = coreSprintFactory.GetTrelloFacade();
            _cardHelper = coreSprintFactory.GetCardHelper();
            _spreadsheetFacade = coreSprintFactory.GetSpreadsheetFacade();
            _sprintRunningHelper = coreSprintFactory.GetSprintRunningHelper();
            _telegramHelper = coreSprintFactory.GetTelegramHelper();
        }

        public override string Name { get; } = "card_info";

        public override void Execute(Message message)
        {
            var queryCards = GetQueryCards(message.Text).ToList();
            var queryResponsible = _telegramHelper.GetQueryResponsible(message.Text, Name).ToList();
            var chatId = message.Chat.Id;

            if (queryCards.Any() || queryResponsible.Any())
            {
                Console.WriteLine("Consultando cart�es...");
                var cards = GetCards();

                var filteredCards = FilterCards(cards, queryCards, queryResponsible).AsParallel();

                SendToChat(chatId,
                    $"Foram encontrados {filteredCards.Count()} cart�es que atendem aos crit�rios consultados.\r\n" +
                    "Aguarde um pouco enquanto processo as informa��es destes cart�es...");

                if (filteredCards.Any())
                {
                    filteredCards.ForAll(card => SendToChat(chatId, MountCardInfo(card)));
                    SendToChat(chatId, $"Todos os cart�es foram listados em resposta ao comando \"{message.Text}\".");
                }
                else
                {
                    SendToChat(chatId,
                        $"N�o foi encontrado nenhum cart�o para os respons�veis \"{(queryResponsible.Any() ? queryResponsible.Aggregate((text, next) => text + "; " + next) : "")}\"" +
                        $" que correspondam a consulta \"{(queryCards.Any() ? queryCards.Aggregate((text, next) => text + "; " + next) : "")}\"");
                }

                return;
            }

            SendToChat(chatId, "Informe um par�metro com a identifica��o do(s) cart�o(�es) para o comando.");
        }

        private IEnumerable<Card> GetCards()
        {
            return ExecutionHelper.ExecuteAndRetryOnFail(() => _trelloFacade.GetCards(_trelloBoardId));
        }

        private static IEnumerable<string> GetQueryCards(string message)
        {
            var messageText = message.Split(' ').ToList();
            messageText.RemoveAt(0);

            var queryCards = messageText.Any()
                ? messageText.Aggregate((text, next) => text + " " + next)
                    .Split('\"')
                    .Where(s => !string.IsNullOrWhiteSpace(s)).Distinct()
                : new List<string>();
            return queryCards;
        }

        private IEnumerable<Card> FilterCards(IEnumerable<Card> cards, IEnumerable<string> queryCards, IEnumerable<string> queryResponsible)
        {
            Console.WriteLine("Filtrando cart�es...");

            var enumerableQueryCards = queryCards as IList<string> ?? queryCards.ToList();
            var qResponsible = queryResponsible as IList<string> ?? queryResponsible.ToList();

            cards = enumerableQueryCards.Any()
                ? cards.Where(c => enumerableQueryCards.Any(q => c.Url.ToLower().Contains(q.ToLower()) || c.Name.ToLower().Contains(q.ToLower())))
                : cards;

            cards = qResponsible.Any()
                ? cards.Where(c =>
                {
                    var cardResponsible = ExecutionHelper.ExecuteAndRetryOnFail(() => _cardHelper.GetResponsible(c));
                    return cardResponsible.Split(';').Any(r => qResponsible.Any(qr => r.ToLower().Contains(qr.ToLower()) || qr.ToLower().Contains(r.ToLower())));
                })
                : cards;

            return cards;
        }

        private string MountCardInfo(Card card)
        {
            Console.WriteLine("Montando resposta para o cart�o: {0}", card.Name);

            const string worksheetName = "SprintCorrente";
            var worksheet = ExecutionHelper.ExecuteAndRetryOnFail(() => _spreadsheetFacade.GetWorksheet(_spreadsheetId, worksheetName));

            var sprintPeriod = ExecutionHelper.ExecuteAndRetryOnFail(() => _sprintRunningHelper.GetSprintPeriod(worksheet));
            var startDate = sprintPeriod["startDate"];
            var endDate = sprintPeriod["endDate"];

            var cardName = _cardHelper.GetCardTitle(card);
            var estimate = _cardHelper.GetCardEstimate(card);
            var comments = ExecutionHelper.ExecuteAndRetryOnFail(() => _cardHelper.GetCardComments(card)).ToList();
            var workedAndPendingBeforeSprint = _cardHelper.GetWorkedAndPending(estimate, comments, startDate);
            var workedAndPending = _cardHelper.GetWorkedAndPending(estimate, comments, endDate);
            var workedAndPendingInSprint = _cardHelper.GetWorkedAndPending(estimate, comments, startDate, endDate);
            var workedInSprint = workedAndPendingInSprint["worked"].ToString(CultureInfo.InvariantCulture);
            var worked = workedAndPending["worked"].ToString(CultureInfo.InvariantCulture).Replace(".", ",");
            var pending = workedAndPending["pending"].ToString(CultureInfo.InvariantCulture).Replace(".", ",");
            var pendingBeforeSprint = workedAndPendingBeforeSprint["pending"].ToString(CultureInfo.InvariantCulture).Replace(".", ",");
            var status = _cardHelper.GetStatus(card);
            var responsibles = _cardHelper.GetResponsible(card);

            //TODO: ajustar ordem
            var message = string.Format(
                "Cart�o: {0} ({1})\r\n-------------------\r\nRespons�vel: {6}\r\nStatus: {5}\r\nEstimado: {2}\r\nTrabalhado: {3}\r\nTrabalhado no Sprint: {7}\r\nPendente no in�cio do Sprint: {8}\r\nPendente: {4}",
                cardName, card.Url, estimate, worked, pending, status, responsibles, workedInSprint, pendingBeforeSprint);

            return message;
        }
    }
}