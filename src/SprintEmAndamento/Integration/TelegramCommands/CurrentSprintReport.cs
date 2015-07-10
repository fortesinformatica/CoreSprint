using System;
using System.Collections.Generic;
using System.Text;
using CoreSprint.Factory;
using CoreSprint.Spreadsheet;
using NetTelegramBotApi;
using NetTelegramBotApi.Requests;

namespace CoreSprint.Integration.TelegramCommands
{
    public class CurrentSprintReport : ITelegramCommand
    {
        private readonly TelegramBot _telegramBot;
        private readonly string _spreadsheetId;
        private readonly ISpreadsheetFacade _spreadsheetFacade;

        public CurrentSprintReport(TelegramBot telegramBot, ICoreSprintFactory factory, string spreadsheetId)
        {
            _telegramBot = telegramBot;
            _spreadsheetId = spreadsheetId;
            _spreadsheetFacade = factory.GetSpreadsheetFacade();
        }

        public void Execute(long chatId)
        {
            Console.WriteLine("Consultando relatório do sprint corrente...");

            const string worksheetName = "SprintCorrente";

            var spreadsheet = _spreadsheetFacade.GetSpreadsheet(_spreadsheetId);
            var worksheet = _spreadsheetFacade.GetWorksheet(spreadsheet, worksheetName);

            //TODO: deduzir linhas e colunas max e min
            var reportCells = _spreadsheetFacade.GetCellValue(worksheet, 32, 38, 1, 7);

            var i = 1;
            var stringBuffer = new StringBuilder("Relatório do Sprint\r\n=================");
            var headers = new List<string>();
            foreach (var reportCell in reportCells)
            {
                if (i < 7)
                {
                    headers.Add(reportCell.Value);
                }
                else
                {
                    var value = i % 7 == 0
                        ? string.Format("\r\n{0}\r\n{1}", reportCell.Value, "------------------")
                        : string.Format("{0}: {1}", headers[(i % 7) - 1], reportCell.Value);

                    stringBuffer.Append(value + "\r\n");
                    Console.WriteLine(value);
                }

                i++;
            }
            Console.WriteLine();

            var sendMessage = new SendMessage(chatId, stringBuffer.ToString());
            Console.WriteLine("Enviando mensagem para o chat...");
            var result = _telegramBot.MakeRequestAsync(sendMessage).Result;

            if (result == null)
                Console.WriteLine("Erro: Não foi possível enviar a mensagem para o chat!");
            else
                Console.WriteLine("Mensagem enviada!");
        }
    }
}