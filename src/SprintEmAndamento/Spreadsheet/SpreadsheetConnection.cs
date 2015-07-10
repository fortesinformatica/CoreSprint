using Google.GData.Client;
using Google.GData.Spreadsheets;

namespace CoreSprint.Spreadsheet
{
    public class SpreadsheetConnection : ISpreadsheetConnection
    {
        public SpreadsheetsService SpreadsheetService { get; private set; }

        public SpreadsheetConnection(string userToken, string refreshToken)
        {
            Authenticate(userToken, refreshToken);
        }

        private void Authenticate(string userToken, string refreshToken)
        {
            var parameters = new OAuth2Parameters
            {
                ClientId = SpreadsheetConfiguration.ClientId,
                ClientSecret = SpreadsheetConfiguration.ClientSecret,
                RedirectUri = SpreadsheetConfiguration.RedirectUri,
                Scope = SpreadsheetConfiguration.Scope,
                RefreshToken = refreshToken,
                AccessToken = userToken
            };

            SpreadsheetService = new SpreadsheetsService(CoreSprintApp.GoogleApiAppName)
            {
                RequestFactory = new GOAuth2RequestFactory(null, CoreSprintApp.GoogleApiAppName, parameters)
            };
        }
    }
}