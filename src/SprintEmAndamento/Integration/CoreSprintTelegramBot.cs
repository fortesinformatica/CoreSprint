﻿using System;
using System.Collections.Generic;
using System.IO;
using CoreSprint.Factory;
using CoreSprint.Telegram;
using CoreSprint.Telegram.TelegramCommands;
using NetTelegramBotApi;
using NetTelegramBotApi.Requests;
using NetTelegramBotApi.Types;

namespace CoreSprint.Integration
{
    public class CoreSprintTelegramBot : ICommand
    {
        private readonly CoreSprintFactory _sprintFactory;
        private readonly TelegramBot _telegramBot;

        public CoreSprintTelegramBot(CoreSprintFactory sprintFactory)
        {
            _sprintFactory = sprintFactory;

            if (!TelegramConfiguration.HasConfiguration())
                TelegramConfiguration.Configure();

            var telegramBotToken = TelegramConfiguration.GetConfiguration()["botToken"];

            _telegramBot = new TelegramBot(telegramBotToken);
        }

        private IDictionary<string, ITelegramCommand> GetCommands()
        {
            /*
             * report - Relatório do sprint atual com horas trabalhadas e pendentes por profissional
             * update_report - Atualiza planilha do sprint atual com as informações do quadro de Sprint do Trello
             * update_cards_report - Atualiza lista de cartões do Trello na planilha do sprint atual
             * update_work_extract - Atualiza a planilha de horas trabalhadas com o extrato do sprint
             */
            return new Dictionary<string, ITelegramCommand>
            {
                {"/report", new TelegramCurrentSprintReport(_telegramBot, _sprintFactory, CoreSprintApp.SpreadsheetId)},
                {"/update_report", new TelegramCurrentSprintUpdate(_telegramBot, _sprintFactory, CoreSprintApp.TrelloBoardId, CoreSprintApp.SpreadsheetId)},
                {"/update_cards_report", new TelegramListSprintCards(_telegramBot, _sprintFactory, CoreSprintApp.TrelloBoardId, CoreSprintApp.SpreadsheetId)},
                {"/update_work_extract", new TelegramWorkExtractUpdate(_telegramBot, _sprintFactory, CoreSprintApp.TrelloBoardId, CoreSprintApp.SpreadsheetId)}
            };
        }

        public void Execute()
        {
            //NetTelegramBotApi.Requests
            var updates = GetUpdates();

            foreach (var update in updates)
            {
                var telegramCommands = GetCommands();
                SetLastUpdateId(update.UpdateId);

                foreach (var userCommand in telegramCommands.Keys)
                {
                    if (update.Message.Text.ToLower().Trim().StartsWith(userCommand.Trim().ToLower()))
                    {
                        var command = telegramCommands[userCommand];
                        try
                        {
                            SayCommandReceived(command, update, update.Message.Text);
                            command.Execute(update.Message);
                        }
                        catch (Exception e)
                        {
                            var msgError = string.Format("Ocorreu um erro ao executar o comando: {0}\r\n{1}", e.Message, e.StackTrace);
                            Console.WriteLine(msgError);
                            command.SendToChat(update.Message.Chat.Id, "Ocorreu um erro ao executar o comando!");
                        }
                    }
                }
            }
        }

        private static void SayCommandReceived(ITelegramCommand command, Update update, string userCommand)
        {
            command.SendToChat(update.Message.Chat.Id,
                string.Format("{0}, reconheço seu comando \"{1}\".\r\nPor favor, aguarde um momento enquanto processo...",
                    update.Message.From.FirstName, userCommand));
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
