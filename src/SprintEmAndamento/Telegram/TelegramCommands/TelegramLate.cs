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
        private readonly ICommand _runningSprintUpdater;
        private readonly ISprintRunningHelper _sprintRunningHelper;
        private readonly ISpreadsheetFacade _spreadsheetFacade;

        public TelegramLate(TelegramBot telegramBot, ICoreSprintFactory coreSprintFactory, string trelloBoardId, string spreadsheetId) : base(telegramBot)
        {
            _spreadsheetId = spreadsheetId;
            _runningSprintUpdater = coreSprintFactory.GetRunningSprintUpdater(trelloBoardId, spreadsheetId);
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
                _runningSprintUpdater.Execute();
            }

            messageText = MountMessageResponse(Process());
            SendToChat(message.Chat.Id, messageText);
        }

        public void Execute(IEnumerable<long> chats)
        {
            _runningSprintUpdater.Execute();

            var messageText = MountMessageResponse(Process());
            chats.AsParallel().ForAll(chatId => SendToChat(chatId, messageText));
        }

        private static string MountMessageResponse(IEnumerable<string> messages)
        {
            return messages != null && messages.Any()
                ? messages.Aggregate((current, next) => $"{current}\r\n{next}")
                : "N�o existe pend�ncia de trabalho superior a disponibilidade para nenhum profissional";
        }

        private IEnumerable<string> Process()
        {
            Console.WriteLine("Consultando informa��es de disponibilidade na planilha...");
            var worksheet = ExecutionHelper.ExecuteAndRetryOnFail(() => _spreadsheetFacade.GetWorksheet(_spreadsheetId, "SprintCorrente")); //TODO: transformar string literal em uma constante da aplica��o
            var availabilities = ExecutionHelper.ExecuteAndRetryOnFail(() => _sprintRunningHelper.GetAvailabilityFromNow(worksheet));

            Console.WriteLine("Consultando informa��es do relat�rio de andamento do sprint na planilha...");
            var messages = (from availability in availabilities
                            let report =
                                ExecutionHelper.ExecuteAndRetryOnFail(() => _sprintRunningHelper.GetReportFromSection(worksheet, "Relat�rio de andamento do sprint", availability.Key))
                            let pending = double.Parse(report["Trabalho alocado pendente"])
                            let professional = report["title"]
                            where availability.Value < pending
                            select $"* {professional} possui {availability.Value} hora(s) dispon�veis at� o fim do sprint, mas ainda existem {pending} hora(s) de trabalho pendente.")
                .ToList();
            return messages;
        }
    }
}