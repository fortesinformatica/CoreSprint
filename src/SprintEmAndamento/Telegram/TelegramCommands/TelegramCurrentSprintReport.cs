using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreSprint.Factory;
using CoreSprint.Helpers;
using CoreSprint.Spreadsheet;
using Google.GData.Spreadsheets;
using NetTelegramBotApi;
using NetTelegramBotApi.Types;

namespace CoreSprint.Telegram.TelegramCommands
{
    public class TelegramCurrentSprintReport : TelegramCommand
    {
        private readonly string _spreadsheetId;
        private readonly ISpreadsheetFacade _spreadsheetFacade;
        private readonly ISprintRunningHelper _sprintRunningHelper;
        private readonly ITelegramHelper _telegramHelper;

        public TelegramCurrentSprintReport(TelegramBot telegramBot, ICoreSprintFactory factory, string spreadsheetId)
            : base(telegramBot)
        {
            _spreadsheetId = spreadsheetId;
            _spreadsheetFacade = factory.GetSpreadsheetFacade();
            _sprintRunningHelper = factory.GetSprintRunningHelper();
            _telegramHelper = factory.GetTelegramHelper();
        }

        public override void Execute(Message message)
        {
            Console.WriteLine("Consultando relatório do sprint corrente...");

            const string worksheetName = "SprintCorrente";

            var spreadsheet = _spreadsheetFacade.GetSpreadsheet(_spreadsheetId);
            var worksheet = _spreadsheetFacade.GetWorksheet(spreadsheet, worksheetName);
            var professionals = _telegramHelper.GetQueryResponsible(message.Text, "report");

            //TODO: deduzir linhas e colunas max e min ao invés de utilizar valores fixos
            var sprintRunningSection = _sprintRunningHelper.GetSectionLinesPosition(worksheet, "Relatório de andamento do sprint");
            var sectionFirstLine = sprintRunningSection.Min(s => s.Value) - 1;
            var sectionLastLine = sprintRunningSection.Max(s => s.Value) + 1;
            var sectionHeader = _spreadsheetFacade.GetCellsValues(worksheet, sectionFirstLine, sectionFirstLine, 1, uint.MaxValue);
            var sectionColumnLastHeader = sectionHeader.Max(s => s.Column);

            var reportCells = _spreadsheetFacade.GetCellsValues(worksheet, sectionFirstLine, sectionLastLine, 1, sectionColumnLastHeader);

            var enumerableProfessionals = professionals as string[] ?? professionals.ToArray();
            var report = GetReport(reportCells, enumerableProfessionals.Any() ? enumerableProfessionals : new[] { "total" }, sectionColumnLastHeader);

            //add header
            var headerReport = _spreadsheetFacade.GetCellsValues(worksheet, 2, 5, 1, 2);
            var strReport = new StringBuilder($"Relatório do Sprint => {report["title"]}\r\n=============================================\r\n");
            var availability = _sprintRunningHelper.GetAvailabilityFromNow(worksheet, enumerableProfessionals).Sum(av => av.Value);
            var i = 0;

            foreach (var headerValue in headerReport)
            {
                strReport.Append(i++ % 2 == 0
                    ? $"{headerValue.Value} => "
                    : $"{headerValue.Value}\r\n");
            }

            strReport.Append($"Horas disponíveis => {availability}\r\n");

            foreach (var keyPar in report.Where(keyPar => !keyPar.Key.Equals("title")))
            {
                strReport.Append($"{keyPar.Key} => {keyPar.Value}\r\n");
            }

            SendToChat(message.Chat.Id, strReport.ToString());
        }

        private static IDictionary<string, string> GetReport(IEnumerable<CellEntry> reportCells, IEnumerable<string> professionals, uint sectionColumnLastHeader)
        {
            var i = 1;
            var headers = new List<string>();
            var addToReport = false;
            var report = new Dictionary<string, string>();

            foreach (var reportCell in reportCells)
            {
                if (i < sectionColumnLastHeader)
                {
                    headers.Add(reportCell.Value);
                }
                else
                {
                    if (i % sectionColumnLastHeader == 0) //nova linha
                    {
                        var lowerValue = reportCell.Value.ToLower().Trim();
                        var enumerableParams = professionals as string[] ?? professionals.ToArray();
                        addToReport = !enumerableParams.Any() || enumerableParams.Any(p => lowerValue.Contains(p) || p.Contains(lowerValue));

                        if (addToReport)
                            report["title"] = reportCell.Value;
                    }
                    else if (addToReport) //mesma linha
                    {
                        report.Add(headers[(int)((i % sectionColumnLastHeader) - 1)], reportCell.Value);
                    }
                }

                i++;
            }
            return report;
        }
    }
}