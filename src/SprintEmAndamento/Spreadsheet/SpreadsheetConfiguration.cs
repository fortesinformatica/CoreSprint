using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using DialectSoftware.Web.HtmlAgilityPack;
using Google.GData.Client;
using Google.GData.Spreadsheets;
using mshtml;
using SHDocVw;

namespace CoreSprint.Spreadsheet
{
    public static class SpreadsheetConfiguration
    {
        public static string ClientId { get; set; }
        public static string ClientSecret { get; set; }
        public static string Scope { get; set; }
        public static string RedirectUri { get; set; }

        public static void Configure()
        {
            Console.WriteLine("Configurando integração com Google Planilhas...");

            ClientId = "1067851659930-r4gdvb352t81tu35anuj070dhul9s69v.apps.googleusercontent.com";
            ClientSecret = "fQaBk3kjpRBKXt2WjJHJkxkE";
            Scope = "https://spreadsheets.google.com/feeds https://docs.google.com/feeds";
            RedirectUri = "urn:ietf:wg:oauth:2.0:oob";

            var parameters = GetParameters();
            var requestOk = false;

            if (File.Exists(CoreSprintApp.SpreadsheetConfigPath))
            {
                var configuration = GetConfiguration();
                parameters.RefreshToken = configuration["refreshToken"];
                OAuthUtil.RefreshAccessToken(parameters);
                requestOk = TestRequest(parameters);
            }

            if (!requestOk)
            {
                parameters = GetParameters();
                var authorizationUrl = OAuthUtil.CreateOAuth2AuthorizationUrl(parameters);
                parameters.AccessCode = GetAccessCode(authorizationUrl);
                OAuthUtil.GetAccessToken(parameters);
            }

            File.WriteAllLines(CoreSprintApp.SpreadsheetConfigPath, new List<string> { parameters.AccessToken, parameters.RefreshToken });
            Console.WriteLine("\r\nConfiguração do Google Planilhas finalizada!");
        }

        private static string GetAccessCode(string authorizationUrl)
        {
            var accessCode = RetrieveAccessCode(authorizationUrl);

            if (string.IsNullOrWhiteSpace(accessCode))
            {
                Console.WriteLine(authorizationUrl);
                Process.Start(authorizationUrl);
                Console.WriteLine(
                    "\r\nAcesse a URL acima para recuperar sua chave de acesso para as Planilhas do Google.");
                Console.Write("\r\nInsira o código de acesso gerado na URL aqui: ");

                accessCode = Console.ReadLine();
            }
            return accessCode;
        }

        private static string RetrieveAccessCode(string authorizationUrl)
        {
            Process.Start("iexplore", authorizationUrl);
            string appKey = null;
            var shellWindows = new ShellWindows();
            var htmlDocument = new HtmlDocument();
            var ie = GetInternetExplorerInstance(shellWindows);

            var document = ie.Document as HTMLDocument;

            if (document != null)
            {
                HtmlNode elementById = null;
                while (document.readyState != "complete" || elementById == null || !ie.LocationURL.Contains("accounts.google.com/o/oauth2/approval"))
                {
                    htmlDocument.LoadHtml(document.documentElement.innerHTML);
                    elementById = htmlDocument.GetElementById("code");
                    Thread.Sleep(500);
                }
                appKey = elementById.Attributes["value"].Value;
            }

            ie.Quit();
            return appKey;
        }

        public static Dictionary<string, string> GetConfiguration()
        {
            if (File.Exists(CoreSprintApp.SpreadsheetConfigPath))
            {
                var configLines = File.ReadAllLines(CoreSprintApp.SpreadsheetConfigPath);
                return new Dictionary<string, string> { { "accessToken", configLines[0] }, { "refreshToken", configLines[1] } };
            }
            throw new Exception("Você ainda não configurou a integração com o Google Planilhas.");
        }

        public static bool HasConfiguration()
        {
            if (File.Exists(CoreSprintApp.SpreadsheetConfigPath))
            {
                var configLines = File.ReadAllLines(CoreSprintApp.SpreadsheetConfigPath);
                var hasThreeLines = configLines.Length > 2;
                var parameters = GetParameters();

                parameters.AccessToken = configLines[0];
                parameters.RefreshToken = configLines[1];

                return hasThreeLines && TestRequest(parameters);
            }

            return false;
        }

        private static bool TestRequest(OAuth2Parameters parameters)
        {
            try
            {
                var spreadsheetService = new SpreadsheetsService(CoreSprintApp.GoogleApiAppName)
                {
                    RequestFactory = new GOAuth2RequestFactory(null, CoreSprintApp.GoogleApiAppName, parameters)
                };

                var spreadsheetQuery = new SpreadsheetQuery();
                spreadsheetService.Query(spreadsheetQuery);

                return true;
            }
            catch (GDataRequestException e)
            {
                var message = e.InnerException != null ? e.InnerException.Message : "";
                if (message.Contains("(401) Unauthorized"))
                    return false;
                throw;
            }
        }

        private static OAuth2Parameters GetParameters()
        {
            return new OAuth2Parameters
            {
                ClientId = ClientId,
                ClientSecret = ClientSecret,
                RedirectUri = RedirectUri,
                Scope = Scope
            };
        }

        private static InternetExplorer GetInternetExplorerInstance(ShellWindows shellWindows)
        {
            var ie = shellWindows.OfType<InternetExplorer>().FirstOrDefault(ieInstance => ieInstance.LocationURL.Contains("accounts.google.com/o/oauth2"));
            while (ie == null)
                ie = shellWindows.OfType<InternetExplorer>().FirstOrDefault(ieInstance => ieInstance.LocationURL.Contains("accounts.google.com/o/oauth2"));
            return ie;
        }
    }
}