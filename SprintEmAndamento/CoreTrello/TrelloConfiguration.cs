using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TrelloNet;

namespace CoreSprint.CoreTrello
{
    public static class TrelloConfiguration
    {
        private static string _trelloConfig = "c:\\temp\\trello.config";

        public static void Configure()
        {
            var urlAppKey = "https://trello.com/app-key";
            Process.Start(urlAppKey);
            Console.Write("Acesse {0} e insira a sua chave de desenvolvedor: ", urlAppKey);
            var appKey = Console.ReadLine();

            var trello = new Trello(appKey);
            var urlUserToken = trello.GetAuthorizationUrl(Constants.TrelloAppName, Scope.ReadOnly, Expiration.Never);

            Process.Start(urlUserToken.ToString());
            Console.WriteLine("\r\n{0}", urlUserToken);
            Console.WriteLine("\r\nAgora acesse a URL acima para gerar seu token de acesso aos quadros privados do Trello.");
            Console.Write("\r\nInforme aqui o token gerado pela URL: ");
            var userToken = Console.ReadLine();

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