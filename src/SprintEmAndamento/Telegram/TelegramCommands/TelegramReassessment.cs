using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CoreSprint.Extensions;
using CoreSprint.Factory;
using CoreSprint.Helpers;
using CoreSprint.Spreadsheet;
using CoreSprint.Trello;
using NetTelegramBotApi;
using NetTelegramBotApi.Types;
using TrelloNet;

namespace CoreSprint.Telegram.TelegramCommands
{
    public class TelegramReassessment : TelegramCommand, ITelegramProactiveCommand
    {
        private readonly string _trelloBoardId;
        private readonly string _spreadsheetId;
        private readonly ITrelloFacade _trelloFacade;
        private readonly ICardHelper _cardHelper;
        private readonly ISpreadsheetFacade _spreadsheetFacade;
        private readonly ISprintRunningHelper _sprintRunningHelper;

        public TelegramReassessment(TelegramBot telegramBot, ICoreSprintFactory coreSprintFactory, string trelloBoardId, string spreadsheetId) : base(telegramBot)
        {
            _trelloBoardId = trelloBoardId;
            _spreadsheetId = spreadsheetId;
            _trelloFacade = coreSprintFactory.GetTrelloFacade();
            _spreadsheetFacade = coreSprintFactory.GetSpreadsheetFacade();
            _cardHelper = coreSprintFactory.GetCardHelper();
            _sprintRunningHelper = coreSprintFactory.GetSprintRunningHelper();
        }

        public override string Name { get; } = "reassessment";

        public override void Execute(Message message)
        {
            var chatId = message.Chat.Id;
            var messageText = message.Text;

            Execute(new[] { chatId }, messageText);
        }

        public void Execute(IEnumerable<long> chats, string message = "")
        {
            var dateTimeLastExecution = GetDateTimeLastExecution(message);

            if (dateTimeLastExecution == null)
            {
                chats.AsParallel().ForAll(chatId => SendToChat(chatId, $"Formato de data inválido no comando: \"{message}\"!\r\nResposta ao comando {message}."));
            }
            else
            {
                chats.AsParallel().ForAll(chatId =>
                {
                    var reassessments = GetReassessment(dateTimeLastExecution);
                    if (reassessments.Any())
                        reassessments.AsParallel().ForAll(reassessment => SendToChat(chatId, reassessment));
                    else
                        SendToChat(chatId, $"Não houve aumento de estimativa dos cartões no sprint após a data {dateTimeLastExecution.Value.ToHumanReadable()}.");
                });
            }
        }

        private List<string> GetReassessment(DateTime? dateTimeLastExecution)
        {
            var cards = ExecutionHelper.ExecuteAndRetryOnFail(() => _trelloFacade.GetCards(_trelloBoardId));
            var reassessments = new List<string>();
            var dateTimeNow = DateTime.Now;

            if (dateTimeLastExecution.HasValue)
            {
                var count = 0;
                var enumerableCards = cards as Card[] ?? cards.ToArray();
                var totalCards = enumerableCards.Count();

                enumerableCards.AsParallel().ForAll(card =>
                {
                    var cardName = _cardHelper.GetCardTitle(card);
                    Console.WriteLine($"({++count}/{totalCards}) Avaliando reestimativa de \"{cardName}\"");

                    var comments = ExecutionHelper.ExecuteAndRetryOnFail(() => _cardHelper.GetCardComments(card));
                    var estimate = _cardHelper.GetCardEstimate(card);
                    var commentCardActions = comments as CommentCardAction[] ?? comments.ToArray();
                    var workedAndPendingBefore = ExecutionHelper.ExecuteAndRetryOnFail(() => _cardHelper.GetWorkedAndPending(estimate, commentCardActions, dateTimeLastExecution.Value));
                    var workedAndPendingNow = ExecutionHelper.ExecuteAndRetryOnFail(() => _cardHelper.GetWorkedAndPending(estimate, commentCardActions, dateTimeNow));

                    var reassessmentBefore = workedAndPendingBefore["worked"] + workedAndPendingBefore["pending"];
                    var reassessmentNow = workedAndPendingNow["worked"] + workedAndPendingNow["pending"];
                    var reassessmentDiff = reassessmentNow - reassessmentBefore;

                    if (reassessmentDiff > 0 && double.Parse(estimate) > 0)
                    {
                        var responsible = _cardHelper.GetResponsible(card);
                        var dtHumanReadable = dateTimeLastExecution.Value.ToHumanReadable();
                        reassessments.Add($"Cartão Reestimado depois de {dtHumanReadable} => {cardName} ({card.Url})\r\n\r\n" +
                                          $"Responsável => {responsible}\r\n" +
                                          $"Estimativa Inicial => {estimate}\r\n" +
                                          $"Estimativa até {dtHumanReadable} => {reassessmentBefore}\r\n" +
                                          $"Estimativa atual => {reassessmentNow}\r\n" +
                                          $"Aumento na estimativa => {reassessmentDiff}");
                    }
                });
            }
            return reassessments;
        }

        private DateTime? GetDateTimeLastExecution(string messageText)
        {
            var lastExecutinoTime = messageText.Split(' ').Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            var dateFormat = new CultureInfo("pt-BR", false).DateTimeFormat;
            DateTime? dateTimeLastExecution;

            if (lastExecutinoTime.Count() > 1)
                lastExecutinoTime.RemoveAt(0);

            if (lastExecutinoTime.Any())
            {
                try
                {
                    dateTimeLastExecution = Convert.ToDateTime(lastExecutinoTime.First(), dateFormat);
                }
                catch (FormatException)
                {
                    return null;
                }
            }
            else
            {
                const string worksheetName = "SprintCorrente"; //TODO: transformar numa constante da aplicação
                var worksheet = ExecutionHelper.ExecuteAndRetryOnFail(() => _spreadsheetFacade.GetWorksheet(_spreadsheetId, worksheetName));
                var sprintPeriod = ExecutionHelper.ExecuteAndRetryOnFail(() => _sprintRunningHelper.GetSprintPeriod(worksheet));
                var startDate = sprintPeriod["startDate"];
                dateTimeLastExecution = Convert.ToDateTime(startDate, dateFormat);
            }
            return dateTimeLastExecution;
        }
    }
}