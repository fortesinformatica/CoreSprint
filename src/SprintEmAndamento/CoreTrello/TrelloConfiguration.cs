using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Joins;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading;
using DialectSoftware.Web.HtmlAgilityPack;
using mshtml;
using SHDocVw;
using TrelloNet;

namespace CoreSprint.CoreTrello
{
    public static class TrelloConfiguration
    {
        private static string _trelloConfig = "c:\\temp\\trello.config";
        private static bool navigated = false;


        public static void Configure()
        {
            const string urlAppKey = "https://trello.com/app-key";
            Process.Start("iexplore", urlAppKey);
            var shellWindows = new ShellWindows();
            var ie = shellWindows.OfType<InternetExplorer>().FirstOrDefault(ieInstance => ieInstance.LocationURL == urlAppKey);
            while (ie == null)
                ie = shellWindows.OfType<InternetExplorer>().FirstOrDefault(ieInstance => ieInstance.LocationURL == urlAppKey);
            string appKey = null, userToken = null;
            var document = ie.Document as HTMLDocument;
            var autoResetEvent = new AutoResetEvent(false);
            ie.NavigateComplete2 += (object disp, ref object url) =>
            {
                autoResetEvent.Set();
            };
            var htmlDocument = new HtmlDocument();
            if (document != null)
            {
                htmlDocument.LoadHtml(document.documentElement.innerHTML);
                var elementById = htmlDocument.GetElementById("key");
                if (elementById == null)
                {
                    ie.Navigate("https://trello.com/login");
                    autoResetEvent.WaitOne();
                    autoResetEvent.WaitOne();
                    ie.Navigate(urlAppKey);
                    autoResetEvent.WaitOne();
                    //autoResetEvent.WaitOne();
                }
                while (document.readyState != "complete" || elementById == null)
                {
                    htmlDocument.LoadHtml(document.documentElement.innerHTML);
                    elementById = htmlDocument.GetElementById("key");
                }
                appKey = elementById.Attributes["value"].Value;
            }
            Console.Write("Acesse {0} e insira a sua chave de desenvolvedor: ", urlAppKey);
            if (string.IsNullOrWhiteSpace(appKey))
                appKey = Console.ReadLine();

            var trello = new Trello(appKey);
            var urlUserToken = trello.GetAuthorizationUrl(Constants.TrelloAppName, Scope.ReadOnly, Expiration.Never);

            ie.Navigate2(urlUserToken.ToString());
            autoResetEvent.WaitOne();
            autoResetEvent.WaitOne();
            Console.WriteLine(ie.LocationURL);
            //autoResetEvent.WaitOne();
            do
                htmlDocument.LoadHtml(document.documentElement.innerHTML);
            while (document.readyState != "complete");
            userToken = htmlDocument.DocumentNode.SelectSingleNode("//pre").InnerText.Trim();

            ie.Quit();

            Console.WriteLine("\r\n{0}", urlUserToken);
            Console.WriteLine("\r\nAgora acesse a URL acima para gerar seu token de acesso aos quadros privados do Trello.");
            Console.Write("\r\nInforme aqui o token gerado pela URL: ");
            if (string.IsNullOrWhiteSpace(userToken))
                userToken = Console.ReadLine();

            File.WriteAllLines(_trelloConfig, new List<string> { appKey, userToken });

            Console.WriteLine("\r\nConfiguração do Trello finalizada!");
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
    }
}