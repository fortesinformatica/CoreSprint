using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreSprint.Factory;
using CoreSprint.Spreadsheet;
using Google.GData.Spreadsheets;
using NetTelegramBotApi;
using NetTelegramBotApi.Requests;
using NetTelegramBotApi.Types;

namespace CoreSprint.Integration.TelegramCommands
{
    public class TelegramCurrentSprintReport : TelegramCommand
    {
        private readonly string _spreadsheetId;
        private readonly ISpreadsheetFacade _spreadsheetFacade;

        public TelegramCurrentSprintReport(TelegramBot telegramBot, ICoreSprintFactory factory, string spreadsheetId) : base(telegramBot)
        {
            _spreadsheetId = spreadsheetId;
            _spreadsheetFacade = factory.GetSpreadsheetFacade();
        }

        public override void Execute(Message message)
        {
            SpreadsheetConfiguration.Configure();
            Console.WriteLine("Consultando relatório do sprint corrente...");

            const string worksheetName = "SprintCorrente";

            var chatId = message.Chat.Id;
            var spreadsheet = _spreadsheetFacade.GetSpreadsheet(_spreadsheetId);
            var worksheet = _spreadsheetFacade.GetWorksheet(spreadsheet, worksheetName);
            var @params = GetParams(message);

            //TODO: deduzir linhas e colunas max e min ao invés de utilizar valores fixos
            var reportCells = _spreadsheetFacade.GetCellsValues(worksheet, 32, 38, 1, 7);
            var messageResult = GetReport(reportCells, @params);

            SendToChat(chatId, messageResult);
        }

        private static IEnumerable<string> GetParams(Message message)
        {
            var @params = message.Text.Split(' ').ToList();
            @params.RemoveAt(0);
            return @params.Select(p => p.ToLower().Trim());
        }

        private static string GetReport(IEnumerable<CellEntry> reportCells, IEnumerable<string> @params)
        {
            //TODO: deduzir linhas e colunas max e min ao invés de utilizar valores fixos
            var stringBuffer = new StringBuilder("Relatório do Sprint\r\n=================");
            var i = 1;
            var headers = new List<string>();
            var addToReport = false;

            foreach (var reportCell in reportCells)
            {
                if (i < 7)
                {
                    headers.Add(reportCell.Value);
                }
                else
                {
                    var value = "";

                    if (i % 7 == 0) //nova linha
                    {
                        var lowerValue = reportCell.Value.ToLower().Trim();
                        addToReport = !@params.Any() || @params.Any(p => lowerValue.Contains(p) || p.Contains(lowerValue));
                        value = string.Format("\r\n{0}\r\n{1}", reportCell.Value, "------------------");
                    }
                    else //mesma linha
                    {
                        value = string.Format("{0}: {1}", headers[(i % 7) - 1], reportCell.Value);
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