using CoreSprint.CoreSpreadsheet;
using CoreSprint.CoreTrello;

namespace CoreSprint
{
    //TODO: Substituir por injetor de dependências
    public class SprintFactory
    {
        public TrelloSprint GetTrelloSprint()
        {
            var trelloConfiguration = TrelloConfiguration.GetConfiguration();
            var trelloAppKey = trelloConfiguration["appKey"];
            var trelloUserToken = trelloConfiguration["userToken"];

            var trelloConn = new TrelloConnection(trelloAppKey, trelloUserToken);
            return new TrelloSprint(trelloConn);
        }

        public SpreadsheetSprint GetSpreadsheetSprint()
        {
            var spreadsheetConfiguration = SpreadsheetConfiguration.GetConfiguration();
            var googleApiUserToken = spreadsheetConfiguration["accessToken"];
            var googleApiRefreshToken = spreadsheetConfiguration["refreshToken"];
            var spreadsheetConn = new SpreadsheetConnection(googleApiUserToken, googleApiRefreshToken);
            return new SpreadsheetSprint(spreadsheetConn);
        }
    }
}
