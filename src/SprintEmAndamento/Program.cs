using System;
using System.Collections.Generic;
using CoreSprint.CoreSpreadsheet;
using CoreSprint.CoreTrello;
using CoreSprint.Factory;
using CoreSprint.Integration;

namespace CoreSprint
{
    public class Program 
    {
        static void Main(string[] args)
        {
            ConfigureRemoteIntegrations();

            const string trelloBoardId = "x3EGx2MZ";
            const string spreadsheetId = "1iI6EG4sDnkGSPtuqs8NILoiad1QqzVPq8j-HAHjZxaQ";

            var sprintFactory = new CoreSprintFactory();
            var commandList = new List<ICommand>
            {
                new CurrentSprint(sprintFactory, trelloBoardId, spreadsheetId),
                new ListSprintCards(sprintFactory, trelloBoardId, spreadsheetId)
            };

            commandList.ForEach(c => c.Execute());
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
    }
}
