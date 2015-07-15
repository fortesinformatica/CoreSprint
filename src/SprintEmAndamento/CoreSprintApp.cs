using System;
using CoreSprint.Spreadsheet;
using CoreSprint.Trello;

namespace CoreSprint
{
    public static class CoreSprintApp
    {
        public static string TrelloAppName = "FortesInformatica.Core.SprintEmAndamento";
        public static string GoogleApiAppName = "FortesCoreSprintEmAndamento";
        public static string SpreadsheetConfigPath = "c:\\temp\\CoreSprint\\spreadsheet.config";
        public static string TrelloConfigPath = "c:\\temp\\CoreSprint\\trello.config";
        public static string TelegramConfigPath = "c:\\temp\\CoreSprint\\telegram.config";
        public static string TelegramDataPah = "c:\\temp\\CoreSprint\\telegram.data";
        public static string TrelloBoardId = "x3EGx2MZ";
        public static string SpreadsheetId = "1iI6EG4sDnkGSPtuqs8NILoiad1QqzVPq8j-HAHjZxaQ";

        public static void ConfigureRemoteIntegrations()
        {
            if (!TrelloConfiguration.HasConfiguration())
                TrelloConfiguration.Configure();
            if (!SpreadsheetConfiguration.HasConfiguration())
                SpreadsheetConfiguration.Configure();
        }
    }
}