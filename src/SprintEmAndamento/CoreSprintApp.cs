using System;
using CoreSprint.Spreadsheet;
using CoreSprint.Trello;

namespace CoreSprint
{
    public static class CoreSprintApp
    {
        public static string TrelloAppName = "FortesInformatica.Core.SprintEmAndamento";
        public static string GoogleApiAppName = "FortesCoreSprintEmAndamento";
        public static string TrelloBoardId = "x3EGx2MZ"; //TODO: tornar configurável
        public static string SpreadsheetId = "1iI6EG4sDnkGSPtuqs8NILoiad1QqzVPq8j-HAHjZxaQ"; //TODO: tornar configurável

        public static string RootPathData = "c:\\CoreSprint\\";
        //TODO: substituir esquemas de arquivos por outras formas de persistência
        public static string SpreadsheetConfigPath => RootPathData + "spreadsheet.config";
        public static string TrelloConfigPath => RootPathData + "trello.config";
        public static string TelegramConfigPath => RootPathData + "telegram.config";
        public static string TelegramDataPath => RootPathData + "telegram.data";
        public static string TelegramChatsPath => RootPathData + "telegram_chats.data";
        public static string TelegramProactivePath => RootPathData + "telegram_proactive.data";

        public static string TelegramBotName => "coresprint_bot";

        public static void ConfigureRemoteIntegrations()
        {
            if (!TrelloConfiguration.HasConfiguration())
                TrelloConfiguration.Configure();
            if (!SpreadsheetConfiguration.HasConfiguration())
                SpreadsheetConfiguration.Configure();
        }
    }
}