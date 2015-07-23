using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CoreSprint.Factory;
using CoreSprint.Helpers;
using CoreSprint.Trello;
using NetTelegramBotApi;
using NetTelegramBotApi.Types;
using TrelloNet;

namespace CoreSprint.Telegram.TelegramCommands
{
    public class TelegramCardInfo : TelegramCommand
    {
        private readonly string _trelloBoardId;
        private readonly ITrelloFacade _trelloFacade;
        private readonly ICardHelper _cardHelper;

        public TelegramCardInfo(TelegramBot telegramBot, ICoreSprintFactory coreSprintFactory, string trelloBoardId)
            : base(telegramBot)
        {
            _trelloBoardId = trelloBoardId;
            _trelloFacade = coreSprintFactory.GetTrelloFacade();
            _cardHelper = coreSprintFactory.GetCardHelper();
        }

        public override void Execute(Message message)
        {
            var queryCards = GetQueryCards(message.Text).ToList();
            var queryResponsible = GetQueryResponsible(message.Text).ToList();
            var chatId = message.Chat.Id;

            if (queryCards.Any() || queryResponsible.Any())
            {
                Console.WriteLine("Consultando cartões...");
                var cards = GetCards();

                var filteredCards = FilterCards(cards, queryCards, queryResponsible).AsParallel();

                SendToChat(chatId,
                    string.Format(
                        "Foram encontrados {0} cartões que atendem aos critérios consultados.\r\nAguarde um pouco enquanto processo as informações destes cartões...",
                        filteredCards.Count()));

                if (filteredCards.Any())
                {
                    filteredCards.ForAll(card => SendToChat(chatId, MountCardInfo(card)));
                }
                else
                {
                    SendToChat(chatId,
                        string.Format(
                            "Não foi encontrado nenhum cartão para os responsáveis \"{0}\" que correspondam a consulta \"{1}\"",
                            queryResponsible.Any() ? queryResponsible.Aggregate((text, next) => text + "; " + next) : "",
                            queryCards.Any() ? queryCards.Aggregate((text, next) => text + "; " + next) : ""));
                }

                return;
            }

            SendToChat(chatId, "Informe um parâmetro com a identificação do(s) cartão(ões) para o comando.");
        }

        private IEnumerable<Card> GetCards()
        {
            IEnumerable<Card> cards = null;
            ExecutionHelper.ExecuteAndRetryOnFail(() => cards = _trelloFacade.GetCards(_trelloBoardId));
            return cards;
        }

        private IEnumerable<string> GetQueryResponsible(string message)
        {
            var commandText = message.Split(' ').First(s => !string.IsNullOrWhiteSpace(s));
            commandText = commandText.Replace("/card_info", ""); //TODO: não há garantias que este será o nome do comando
            return commandText.Split('_').Where(s => !string.IsNullOrWhiteSpace(s));
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
            Console.WriteLine("Filtrando cartões...");

            var enumerableQueryCards = queryCards as IList<string> ?? queryCards.ToList();
            var qResponsible = queryResponsible as IList<string> ?? queryResponsible.ToList();

            cards = enumerableQueryCards.Any()
                ? cards.Where(c => enumerableQueryCards.Any(q => c.Url.ToLower().Contains(q.ToLower()) || c.Name.ToLower().Contains(q.ToLower())))  
                : cards;

            cards = qResponsible.Any()
                ? cards.Where(c =>
                {
                    var cardResponsible = "";
                    ExecutionHelper.ExecuteAndRetryOnFail(() => cardResponsible = _cardHelper.GetResponsible(c));
                    return cardResponsible.Split(';').Any(r => qResponsible.Any(qr => r.ToLower().Contains(qr.ToLower()) || qr.ToLower().Contains(r.ToLower())));
                })
                : cards;

            return cards;
        }

        private string MountCardInfo(Card card)
        {
            Console.WriteLine("Montando resposta para o cartão: {0}", card.Name);

            var cardName = _cardHelper.GetCardTitle(card);
            var workedAndRemainder = _cardHelper.GetWorkedAndRemainder(card);
            var estimate = _cardHelper.GetCardEstimate(card);
            var worked = workedAndRemainder["worked"].ToString(CultureInfo.InvariantCulture)
                .Replace(".", ",");
            var remainder =
                workedAndRemainder["remainder"].ToString(CultureInfo.InvariantCulture).Replace(".", ",");
            var status = _cardHelper.GetStatus(card);
            var responsibles = _cardHelper.GetResponsible(card);

            var message = string.Format(
                "Cartão: {0} ({1})\r\n-------------------\r\nResponsável: {6}\r\nStatus: {5}\r\nEstimado: {2}\r\nTrabalhado: {3}\r\nPendente: {4}",
                cardName, card.Url, estimate, worked, remainder, status, responsibles);

            return message;
        }
    }
}