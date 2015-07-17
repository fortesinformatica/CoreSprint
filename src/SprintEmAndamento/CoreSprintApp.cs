using System;
using CoreSprint.Spreadsheet;
using CoreSprint.Trello;

namespace CoreSprint
{
    public static class CoreSprintApp
    {
        public static string TrelloAppName = "FortesInformatica.Core.SprintEmAndamento";
        public static string GoogleApiAppName = "FortesCoreSprintEmAndamento";
        public static string TrelloBoardId = "x3EGx2MZ";
        public static string SpreadsheetId = "1iI6EG4sDnkGSPtuqs8NILoiad1QqzVPq8j-HAHjZxaQ";

        public static string RootPathData = "c:\\temp\\CoreSprint\\";
        public static string SpreadsheetConfigPath { get { return RootPathData + "spreadsheet.config"; } }
        public static string TrelloConfigPath { get { return RootPathData + "trello.config"; } }
        public static string TelegramConfigPath { get { return RootPathData + "telegram.config"; } }
        public static string TelegramDataPath { get { return RootPathData + "telegram.data"; } }

        public static void ConfigureRemoteIntegrations()
        {
            if (!TrelloConfiguration.HasConfiguration())
                TrelloConfiguration.Configure();
            if (!SpreadsheetConfiguration.HasConfiguration())
                SpreadsheetConfiguration.Configure();
        }
    }
}