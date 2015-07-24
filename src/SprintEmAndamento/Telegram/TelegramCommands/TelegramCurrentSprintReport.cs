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

        public TelegramCurrentSprintReport(TelegramBot telegramBot, ICoreSprintFactory factory, string spreadsheetId)
            : base(telegramBot)
        {
            _spreadsheetId = spreadsheetId;
            _spreadsheetFacade = factory.GetSpreadsheetFacade();
            _sprintRunningHelper = factory.GetSprintRunningHelper();
        }

        public override void Execute(Message message)
        {
            Console.WriteLine("Consultando relatório do sprint corrente...");

            const string worksheetName = "SprintCorrente";

            var spreadsheet = _spreadsheetFacade.GetSpreadsheet(_spreadsheetId);
            var worksheet = _spreadsheetFacade.GetWorksheet(spreadsheet, worksheetName);
            var @params = GetParams(message);

            //TODO: deduzir linhas e colunas max e min ao invés de utilizar valores fixos
            var sprintRunningSection = _sprintRunningHelper.GetSectionLinesPosition(worksheet, "Relatório de andamento do sprint");
            var sectionFirstLine = sprintRunningSection.Min(s => s.Value) - 1;
            var sectionLastLine = sprintRunningSection.Max(s => s.Value) + 1;
            var sectionHeader = _spreadsheetFacade.GetCellsValues(worksheet, sectionFirstLine, sectionFirstLine, 1, uint.MaxValue);
            var sectionColumnLastHeader = sectionHeader.Max(s => s.Column);

            var reportCells = _spreadsheetFacade.GetCellsValues(worksheet, sectionFirstLine, sectionLastLine, 1, sectionColumnLastHeader);

            var messageResult = GetReport(reportCells, @params.Any() ? @params : new[] { "total" }, sectionColumnLastHeader);

            //add header
            var headerReport = _spreadsheetFacade.GetCellsValues(worksheet, 2, 4, 1, 2);
            var headerBuffer = new StringBuilder("Relatório do Sprint\r\n=================\r\n");
            var i = 0;
            foreach (var headerValue in headerReport)
            {
                headerBuffer.Append(i++ % 2 == 0
                    ? $"{headerValue.Value}: "
                    : $"{headerValue.Value}\r\n");
            }

            SendToChat(message.Chat.Id, $"{headerBuffer}{messageResult}");
        }

        private static IEnumerable<string> GetParams(Message message)
        {
            var @params = message.Text.Split(' ').ToList();
            @params.RemoveAt(0);
            return @params.Select(p => p.ToLower().Trim());
        }

        private static string GetReport(IEnumerable<CellEntry> reportCells, IEnumerable<string> @params, uint sectionColumnLastHeader)
        {
            //TODO: deduzir linhas e colunas max e min ao invés de utilizar valores fixos
            var stringBuffer = new StringBuilder();
            var i = 1;
            var headers = new List<string>();
            var addToReport = false;

            foreach (var reportCell in reportCells)
            {
                if (i < sectionColumnLastHeader)
                {
                    headers.Add(reportCell.Value);
                }
                else
                {
                    var value = "";

                    if (i % sectionColumnLastHeader == 0) //nova linha
                    {
                        var lowerValue = reportCell.Value.ToLower().Trim();
                        addToReport = !@params.Any() || @params.Any(p => lowerValue.Contains(p) || p.Contains(lowerValue));
                        value = $"\r\n{reportCell.Value}\r\n{"------------------"}";
                    }
                    else //mesma linha
                    {
                        value = $"{headers[(int)((i % sectionColumnLastHeader) - 1)]}: {reportCell.Value}";
                    }

                    if (addToReport)
                    {
                        stringBuffer.Append(value + "\r\n");
                        Console.WriteLine(value);
                    }
                }

                i++;
            }
            Console.WriteLine();
            return stringBuffer.ToString();
        }
    }
}