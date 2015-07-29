using System;
using System.Collections.Generic;
using System.Linq;
using CoreSprint.Factory;
using CoreSprint.Helpers;
using CoreSprint.Integration;
using CoreSprint.Spreadsheet;
using NetTelegramBotApi;
using NetTelegramBotApi.Types;

namespace CoreSprint.Telegram.TelegramCommands
{
    public class TelegramLate : TelegramCommand, ITelegramProactiveCommand
    {
        private readonly string _spreadsheetId;
        private readonly ICommand _currentSprintUpdate;
        private readonly ISprintRunningHelper _sprintRunningHelper;
        private readonly ISpreadsheetFacade _spreadsheetFacade;

        public TelegramLate(TelegramBot telegramBot, ICoreSprintFactory coreSprintFactory, string trelloBoardId, string spreadsheetId) : base(telegramBot)
        {
            _spreadsheetId = spreadsheetId;
            _currentSprintUpdate = coreSprintFactory.GetCurrentSprintUpdate(trelloBoardId, spreadsheetId);
            _sprintRunningHelper = coreSprintFactory.GetSprintRunningHelper();
            _spreadsheetFacade = coreSprintFactory.GetSpreadsheetFacade();
        }

        public override string Name { get; } = "late";

        public override void Execute(Message message)
        {
            var messageText = message.Text;

            if (messageText.ToLower().Contains("update_sprint"))
            {
                SendToChat(message.Chat.Id, "Vou processar o quadro do trello e atualizar a planilha para verificar os atrasados...");
                _currentSprintUpdate.Execute();
            }

            var messages = Process();

            SendToChat(message.Chat.Id, messages.Aggregate((current, next) => $"{current}\r\n{next}"));
        }

        public void Execute(IEnumerable<long> chats)
        {
            _currentSprintUpdate.Execute();

            var messages = Process();
            var messageText = messages.Aggregate((current, next) => $"{current}\r\n{next}");
            chats.AsParallel().ForAll(chatId => SendToChat(chatId, messageText));
        }

        private IEnumerable<string> Process()
        {
            Console.WriteLine("Consultando informações de disponibilidade na planilha...");
            var worksheet = ExecutionHelper.ExecuteAndRetryOnFail(() => _spreadsheetFacade.GetWorksheet(_spreadsheetId, "SprintCorrente")); //TODO: transformar string literal em uma constante da aplicação
            var availabilities = ExecutionHelper.ExecuteAndRetryOnFail(() => _sprintRunningHelper.GetAvailabilityFromNow(worksheet));

            Console.WriteLine("Consultando informações do relatório de andamento do sprint na planilha...");
            var messages = (from availability in availabilities
                            let report =
                                ExecutionHelper.ExecuteAndRetryOnFail(() => _sprintRunningHelper.GetReportFromSection(worksheet, "Relatório de andamento do sprint", availability.Key))
                            let pending = double.Parse(report["Trabalho alocado pendente"])
                            let professional = report["title"]
                            where availability.Value < pending
                            select
                                $"* {professional} possui {availability.Value} hora(s) disponíveis até o fim do sprint, mas ainda existem {pending} hora(s) de trabalho pendente.")
                .ToList();
            return messages;
        }
    }
}