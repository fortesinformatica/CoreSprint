using System;
using System.Collections.Generic;
using System.IO;
using CoreSprint.Factory;
using CoreSprint.Integration.TelegramCommands;
using CoreSprint.Telegram;
using NetTelegramBotApi;
using NetTelegramBotApi.Requests;

namespace CoreSprint.Integration
{
    public class TelegramAlerts : ICommand
    {
        private readonly CoreSprintFactory _sprintFactory;
        private readonly TelegramBot _telegramBot;
        private readonly Dictionary<string, ITelegramCommand> _telegramCommands;

        public TelegramAlerts(CoreSprintFactory sprintFactory)
        {
            _sprintFactory = sprintFactory;

            if (!TelegramConfiguration.HasConfiguration())
                TelegramConfiguration.Configure();

            var telegramBotToken = TelegramConfiguration.GetConfiguration()["botToken"];

            _telegramBot = new TelegramBot(telegramBotToken);

            _telegramCommands = new Dictionary<string, ITelegramCommand>
            {
                {"/sprint_report", new CurrentSprintReport(_telegramBot, _sprintFactory, CoreSprintApp.SpreadsheetId)}
            };
        }

        public void Execute()
        {
            //NetTelegramBotApi.Requests
            var updates = _telegramBot.MakeRequestAsync(new GetUpdates
            {
                Offset = GetLastUpdateId() + 1
            }).Result;

            foreach (var update in updates)
            {
                SetLastUpdateId(update.UpdateId);

                foreach (var userCommand in _telegramCommands.Keys)
                {
                    if (update.Message.Text.Trim().StartsWith(userCommand))
                    {
                        var command = _telegramCommands[userCommand];
                        command.Execute(update.Message);
                    }
                }
            }
        }

        private void SetLastUpdateId(long? updateId)
        {
            File.WriteAllText(CoreSprintApp.TelegramDataPah, updateId.ToString());
        }
        private long GetLastUpdateId()
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
