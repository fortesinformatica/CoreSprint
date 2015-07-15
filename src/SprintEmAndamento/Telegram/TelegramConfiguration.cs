using System;
using System.Collections.Generic;
using System.IO;

namespace CoreSprint.Telegram
{
    public static class TelegramConfiguration
    {
        public static void Configure()
        {
            if (!File.Exists(CoreSprintApp.TelegramConfigPath))
            {
                Console.WriteLine("Configurando integração com Telegram...");
                Console.Write("Informe o token de acesso do BOT do Telegram CoreSprint_BOT: ");
                var botToken = Console.ReadLine();
                File.WriteAllLines(CoreSprintApp.TelegramConfigPath, new List<string> { botToken });
                Console.WriteLine("\r\nConfiguração do Telegram finalizada!");
            }
        }

        public static bool HasConfiguration()
        {
            if (File.Exists(CoreSprintApp.TelegramConfigPath))
            {
                var configLines = File.ReadAllLines(CoreSprintApp.TelegramConfigPath);
                var hasOneLine = configLines.Length == 1;
                return hasOneLine;
            }

            return false;
        }

        public static Dictionary<string, string> GetConfiguration()
        {
            if (HasConfiguration())
                Configure();

            var configLines = File.ReadAllLines(CoreSprintApp.TelegramConfigPath);
            return new Dictionary<string, string> { { "botToken", configLines[0].Trim() } };
        }
    }
}
