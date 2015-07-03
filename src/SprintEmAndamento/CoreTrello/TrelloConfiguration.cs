using System;
using System.Collections.Generic;
using System.IO;
using CoreSprint.BrowserInstrumentation;
using DialectSoftware.Web.HtmlAgilityPack;
using TrelloNet;

namespace CoreSprint.CoreTrello
{
    public static class TrelloConfiguration
    {
        private const string TRELLO_APP_KEY_URL = "https://trello.com/app-key";
        private const string TRELLO_LOGIN_URL = "https://trello.com/login";
        private static string _trelloConfig = "c:\\temp\\trello.config";
        private static bool navigated = false;


        public static void Configure()
        {
			Console.WriteLine("Configurando integração com Trello...");
            string appKey, userToken;
            using (var browser = new BrowserDataRetriever(DataGetter))
            {
                appKey = GetAppKey(browser);

                var trello = new Trello(appKey);
                var urlUserToken = trello.GetAuthorizationUrl(Constants.TrelloAppName, Scope.ReadOnly, Expiration.Never);

                userToken = GetUserToken(browser, urlUserToken);
            }

            File.WriteAllLines(_trelloConfig, new List<string> { appKey, userToken });
            Console.WriteLine("\r\nConfiguração do Trello finalizada!");
        }

        private static string DataGetter(HtmlDocument document)
        {
            var elementById = document.GetElementById("key");
            return elementById != null ? elementById.Attributes["value"].Value : null;
        }

        public static Dictionary<string, string> GetConfiguration()
        {
            if (HasConfiguration())
            {
                var configLines = File.ReadAllLines(_trelloConfig);
                return new Dictionary<string, string> { { "appKey", configLines[0].Trim() }, { "userToken", configLines[1].Trim() } };
            }
            throw new Exception("Você ainda não configurou a integração com o Trello.");
        }

        public static bool HasConfiguration()
        {
            if (File.Exists(_trelloConfig))
            {
                var configLines = File.ReadAllLines(_trelloConfig);
                var hasTwoLines = configLines.Length > 1;

                if (hasTwoLines)
                {
                    try
                    {
                        var trello = new Trello(configLines[0]);
                        trello.Authorize(configLines[1]);
                        trello.Members.Me();
                        return true;
                    }
                    catch (TrelloUnauthorizedException)
                    {
                        return false;
                    }
                }
            }

            return false;
        }

        private static string GetAppKey(IBrowserDataRetriever browser)
        {
            browser.AddStep(TRELLO_APP_KEY_URL);
            browser.AddStep(TRELLO_LOGIN_URL);
            browser.AddStep(null, false);
            browser.AddStep(TRELLO_APP_KEY_URL);

            var appKey = browser.Retrieve();

            if (string.IsNullOrWhiteSpace(appKey))
            {
                Console.Write("Acesse {0} e insira a sua chave de desenvolvedor: ", TRELLO_APP_KEY_URL);
                appKey = Console.ReadLine();
            }
            return appKey;
        }

        private static string GetUserToken(IBrowserDataRetriever browser, Uri urlUserToken)
        {
            browser.ClearSteps();
            browser.WithDataGetter(UserTokenGetter)
                .AddStep(urlUserToken.ToString())
                .AddStep(null);

            var userToken = browser.Retrieve();

            if (string.IsNullOrWhiteSpace(userToken))
            {
                Console.WriteLine("\r\n{0}", urlUserToken);
                Console.WriteLine("\r\nAgora acesse a URL acima para gerar seu token de acesso aos quadros privados do Trello.");
                Console.Write("\r\nInforme aqui o token gerado pela URL: ");
                userToken = Console.ReadLine();
            }
            return userToken;
        }

        private static string UserTokenGetter(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//pre");
            return node == null ? null : node.InnerText.Trim();
        }
    }
}