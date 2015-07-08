using Google.GData.Client;
using Google.GData.Spreadsheets;

namespace CoreSprint.CoreSpreadsheet
{
    public class SpreadsheetConnection : ISpreadsheetConnection
    {
        public SpreadsheetsService SpreadsheetService { get; private set; }

        public SpreadsheetConnection(string accessCode, string userToken, string refreshToken)
        {
            Authenticate(accessCode, userToken, refreshToken);
        }

        private void Authenticate(string accessCode, string userToken, string refreshToken)
        {
            var parameters = new OAuth2Parameters
            {
                ClientId = SpreadsheetConfiguration.ClientId,
                ClientSecret = SpreadsheetConfiguration.ClientSecret,
                RedirectUri = SpreadsheetConfiguration.RedirectUri,
                Scope = SpreadsheetConfiguration.Scope,
                RefreshToken = refreshToken,
                AccessToken = userToken,
                AccessCode = accessCode
            };

            SpreadsheetService = new SpreadsheetsService(Constants.GoogleApiAppName)
            {
                RequestFactory = new GOAuth2RequestFactory(null, Constants.GoogleApiAppName, parameters)
            };
        }
    }
}