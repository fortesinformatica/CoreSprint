using Google.GData.Client;
using Google.GData.Spreadsheets;

namespace CoreSprint.CoreSpreadsheet
{
    public class SpreadsheetConnection
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

            SpreadsheetService = new SpreadsheetsService(Constants.GoogleApiAppName)
            {
                RequestFactory = new GOAuth2RequestFactory(null, Constants.GoogleApiAppName, parameters)
            };
        }
    }
}