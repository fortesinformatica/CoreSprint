using System.Collections.Generic;
using System.Linq;
using CoreSprint.Factory;
using CoreSprint.Helpers;
using CoreSprint.Models;
using CoreSprint.Spreadsheet;
using CoreSprint.Trello;
using NetTelegramBotApi;
using NetTelegramBotApi.Types;

namespace CoreSprint.Telegram.TelegramCommands
{
    public class TelegramWorkingCard : TelegramCommand
    {
        private readonly string _trelloBoardId;
        private readonly string _spreadsheetId;
        private readonly ISpreadsheetFacade _spreadsheetFacade;
        private readonly ISprintRunningHelper _sprintRunningHelper;
        private readonly ITrelloFacade _trelloFacade;
        private readonly ICardHelper _cardHelper;
        private readonly ICommentHelper _commentHelper;
        private readonly ITelegramHelper _telegramHelper;

        public TelegramWorkingCard(TelegramBot telegramBot, ICoreSprintFactory coreSprintFactory, string trelloBoardId, string spreadsheetId)
            : base(telegramBot)
        {
            _trelloBoardId = trelloBoardId;
            _spreadsheetId = spreadsheetId;
            _spreadsheetFacade = coreSprintFactory.GetSpreadsheetFacade();
            _trelloFacade = coreSprintFactory.GetTrelloFacade();
            _sprintRunningHelper = coreSprintFactory.GetSprintRunningHelper();
            _cardHelper = coreSprintFactory.GetCardHelper();
            _commentHelper = coreSprintFactory.GetCommentHelper();
            _telegramHelper = coreSprintFactory.GetTelegramHelper();
        }

        public override string Name { get; } = "card_working";

        public override void Execute(Message message)
        {
            var chatId = message.Chat.Id;
            var professionals = _telegramHelper.GetQueryResponsible(message.Text, Name);
            var sprintWorksheet = ExecutionHelper.ExecuteAndRetryOnFail(() => _spreadsheetFacade.GetWorksheet(_spreadsheetId, "SprintCorrente"));
            var sprintPeriod = ExecutionHelper.ExecuteAndRetryOnFail(() => _sprintRunningHelper.GetSprintPeriod(sprintWorksheet));
            var cards = ExecutionHelper.ExecuteAndRetryOnFail(() => _trelloFacade.GetCards(_trelloBoardId));
            var startDate = sprintPeriod["startDate"];
            var endDate = sprintPeriod["endDate"];

            var enumerable = professionals as string[] ?? professionals.ToArray();

            if (!enumerable.Any())
                SendToChat(chatId, $"Informe o nome de pelo menos um profissional.\r\nExemplo: /{Name}_nomedoprofissional\r\nResposta ao comando /{Name}");

            enumerable.AsParallel().ForAll(professional =>
            {
                var allWork = _cardHelper.GetCardsWorkExtract(cards, startDate, endDate, professional);
                var startedWihoutStop = new List<CardWorkDto>();
                CardWorkDto cardWorkStarted = null;
                var cardLink = "";

                foreach (var work in allWork)
                {
                    var hasStartPattern = _commentHelper.HasStartPattern(work.Comment);

                    if (cardWorkStarted != null && !work.CardLink.Equals(cardLink)) //se mudar de cartão e ainda tiver um iniciar aberto
                    {
                        startedWihoutStop.Add(cardWorkStarted);
                        cardWorkStarted = hasStartPattern ? work : null;
                    }
                    else if (cardWorkStarted != null && hasStartPattern) //se tiver mais de um iniciar sem um parar
                    {
                        startedWihoutStop.Add(cardWorkStarted);
                        cardWorkStarted = work;
                    } else if (_commentHelper.HasStopPattern(work.Comment))
                    {
                        cardWorkStarted = null;
                    } else if (hasStartPattern)
                    {
                        cardWorkStarted = work;
                    }

                    cardLink = work.CardLink;
                }

                if (cardWorkStarted != null)
                    startedWihoutStop.Add(cardWorkStarted);

                if (startedWihoutStop.Any())
                {
                    var msgToChat =
                        startedWihoutStop.Aggregate(
                            $"{startedWihoutStop.First().Professional} iniciou trabalho em:",
                            (current, startedWork) => current + $"\r\n{startedWork.WorkAt} => {startedWork.CardName} ({startedWork.CardLink})");

                    SendToChat(chatId, msgToChat);
                }
                else
                {
                    SendToChat(chatId, $"{professional} não tem trabalho iniciado.");
                }
            });
        }
    }
}