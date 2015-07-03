using System;
using System.Linq;
using CoreSprint.CoreSpreadsheet;
using CoreSprint.CoreTrello;
using CoreSprint.Integration;
using Google.GData.Client;
using Google.GData.Spreadsheets;
using TrelloNet;

namespace CoreSprint
{
    public class Program
    {
        static void Main(string[] args)
        {
            ConfigureRemoteIntegrations();

            const string trelloBoardId = "x3EGx2MZ";
            const string spreadsheetId = "1iI6EG4sDnkGSPtuqs8NILoiad1QqzVPq8j-HAHjZxaQ";
            var sprintFactory = new SprintFactory();

            //lista cartões no quadro do sprint na planilha
            var listSprintCards = new ListSprintCards(sprintFactory, trelloBoardId, spreadsheetId);
            listSprintCards.Execute();
        }

        private static void ConfigureRemoteIntegrations()
        {
            if (!TrelloConfiguration.HasConfiguration())
                TrelloConfiguration.Configure();
            Console.WriteLine();
            if (!SpreadsheetConfiguration.HasConfiguration())
                SpreadsheetConfiguration.Configure();
            Console.WriteLine();
        }

        private static void TestTrello(SprintFactory sprintFactory)
        {
            try
            {
                var trelloBoardId = "x3EGx2MZ";
                var trelloSprintCards = sprintFactory.GetTrelloSprint();

                var trelloBoard = trelloSprintCards.GetBoard(trelloBoardId);
                var trelloCards = trelloSprintCards.GetCards(trelloBoard).ToList();

                Console.WriteLine("{0} cartões no quadro: {1}", trelloCards.Count(), trelloBoard.Name);
                foreach (var card in trelloCards)
                    Console.WriteLine("    {0}", card.Name.Replace("\n", " - "));
            }
            catch (TrelloUnauthorizedException)
            {
                Console.WriteLine("Erro: credenciais inválidas para acesso ao Trello!");
            }
        }

        private static void TestSpreadsheet(SprintFactory sprintFactory)
        {
            try
            {
                var spreadsheetId = "1iI6EG4sDnkGSPtuqs8NILoiad1QqzVPq8j-HAHjZxaQ";
                var spreadsheetSprint = sprintFactory.GetSpreadsheetSprint();
                var spreadsheet = spreadsheetSprint.GetSpreadsheet(spreadsheetId);

                if (spreadsheet != null)
                {
                    spreadsheetSprint.CreateWorksheet(spreadsheet, "Teste");

                    Console.WriteLine(spreadsheet.Title.Text);
                    var worksheets = spreadsheet.Worksheets.Entries;

                    foreach (var worksheet in worksheets.Cast<WorksheetEntry>())
                        Console.WriteLine("    {0} - rows: {1} - cols: {2}", worksheet.Title.Text, worksheet.Rows, worksheet.Cols);
                }
            }
            catch (GDataRequestException e)
            {
                var message = e.InnerException != null ? e.InnerException.Message : "";
                if (message.Contains("(401) Unauthorized"))
                    Console.WriteLine("Erro: credenciais inválidas para acesso ao Google Planilhas!");
                else
                    throw;
            }
        }
    }
}
