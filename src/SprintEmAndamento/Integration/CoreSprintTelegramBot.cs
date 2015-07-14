using System;
using System.Collections.Generic;
using System.IO;
using CoreSprint.Factory;
using CoreSprint.Integration.TelegramCommands;
using CoreSprint.Telegram;
using NetTelegramBotApi.Requests;
using NetTelegramBotApi.Types;

namespace CoreSprint.Integration
{
    public class CoreSprintTelegramBot : ICommand
    {
        private readonly CoreSprintFactory _sprintFactory;
        private readonly NetTelegramBotApi.TelegramBot _telegramBot;
        private readonly Dictionary<string, ITelegramCommand> _telegramCommands;

        public CoreSprintTelegramBot(CoreSprintFactory sprintFactory)
        {
            _sprintFactory = sprintFactory;

            if (!TelegramConfiguration.HasConfiguration())
                TelegramConfiguration.Configure();

            var telegramBotToken = TelegramConfiguration.GetConfiguration()["botToken"];

            _telegramBot = new NetTelegramBotApi.TelegramBot(telegramBotToken);

            _telegramCommands = new Dictionary<string, ITelegramCommand>
            {
                {"/sprint_report", new TelegramCurrentSprintReport(_telegramBot, _sprintFactory, CoreSprintApp.SpreadsheetId)},
                {"/sprint_update", new TelegramCurrentSprintUpdate(_telegramBot, _sprintFactory, CoreSprintApp.TrelloBoardId, CoreSprintApp.SpreadsheetId)}
            };
        }

        public void Execute()
        {
            //NetTelegramBotApi.Requests
            var updates = GetUpdates();

            foreach (var update in updates)
            {
                SetLastUpdateId(update.UpdateId);

                foreach (var userCommand in _telegramCommands.Keys)
                {
                    if (update.Message.Text.Trim().StartsWith(userCommand))
                    {
                        var command = _telegramCommands[userCommand];
                        try
                        {
                            command.Execute(update.Message);
                        }
                        catch (Exception e)
                        {
                            var msgError = string.Format("Ocorreu um erro ao executar o comando: {0}", e.Message);
                            Console.WriteLine(msgError);
                            command.SendToChat(update.Message.Chat.Id, msgError);
                        }
                    }
                }
            }
        }

        private IEnumerable<Update> GetUpdates()
        {
            return _telegramBot.MakeRequestAsync(new GetUpdates
            {
                Offset = GetLastUpdateId() + 1
            }).Result;
        }

        private static void SetLastUpdateId(long? updateId)
        {
            File.WriteAllText(CoreSprintApp.TelegramDataPah, updateId.ToString());
        }
        private static long GetLastUpdateId()
        {
            var updateId = 1L;
            if (File.Exists(CoreSprintApp.TelegramDataPah))
            {
                var lastUpdateId = File.ReadAllText(CoreSprintApp.TelegramDataPah);
                updateId = string.IsNullOrWhiteSpace(lastUpdateId) ? 1 : long.Parse(lastUpdateId);
            }
            return updateId;
        }
    }
}
