using CoreSprint.CoreSpreadsheet;
using CoreSprint.CoreTrello;
using CoreSprint.Helpers;

namespace CoreSprint
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
            var googleApiAccessCode = spreadsheetConfiguration["accessCode"];
            var googleApiUserToken = spreadsheetConfiguration["accessToken"];
            var googleApiRefreshToken = spreadsheetConfiguration["refreshToken"];
            var spreadsheetConn = new SpreadsheetConnection(googleApiAccessCode, googleApiUserToken, googleApiRefreshToken);
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
            return new CardHelper(GetTrelloFacade());
        }

        public IWorksheetHelper GetWorksheetHelper()
        {
            return new WorksheetHelper(GetSpreadsheetFacade());
        }
    }
}
