using CoreSprint.Helpers;
using CoreSprint.Integration;
using CoreSprint.Spreadsheet;
using CoreSprint.Trello;

namespace CoreSprint.Factory
{
    //TODO: Substituir por injetor de dependências
    public class CoreSprintFactory : ICoreSprintFactory
    {
        public ITrelloConnection GetTrelloConnection()
        {
            var trelloConfiguration = TrelloConfiguration.GetConfiguration();
            var trelloAppKey = trelloConfiguration["appKey"];
            var trelloUserToken = trelloConfiguration["userToken"];
            var trelloConn = new TrelloConnection(trelloAppKey, trelloUserToken);
            return trelloConn;
        }

        public ISpreadsheetConnection GetSpreadsheetConnection()
        {
            var spreadsheetConfiguration = SpreadsheetConfiguration.GetConfiguration();
            var googleApiUserToken = spreadsheetConfiguration["accessToken"];
            var googleApiRefreshToken = spreadsheetConfiguration["refreshToken"];
            var spreadsheetConn = new SpreadsheetConnection(googleApiUserToken, googleApiRefreshToken);
            return spreadsheetConn;
        }

        public ITrelloFacade GetTrelloFacade()
        {
            var trelloConn = GetTrelloConnection();
            return new TrelloFacade(trelloConn);
        }

        public ISpreadsheetFacade GetSpreadsheetFacade()
        {
            var spreadsheetConn = GetSpreadsheetConnection();
            return new SpreadsheetFacade(spreadsheetConn);
        }

        public ICardHelper GetCardHelper()
        {
            return new CardHelper(GetTrelloFacade(), GetCommentHelper());
        }

        public ICommentHelper GetCommentHelper()
        {
            return new CommentHelper();
        }

        public IWorksheetHelper GetWorksheetHelper()
        {
            return new WorksheetHelper(GetSpreadsheetFacade());
        }

        public ISprintRunningHelper GetSprintRunningHelper()
        {
            return new SprintRunningHelper(GetSpreadsheetFacade());
        }

        public ITelegramHelper GetTelegramHelper()
        {
            return new TelegramHelper();
        }

        public ICommand GetCurrentSprintUpdate(string trelloBoardId, string spreadsheetId)
        {
            return new CurrentSprintUpdate(this, trelloBoardId, spreadsheetId);
        }
    }
}
