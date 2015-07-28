using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
        private static List<Update> _unprocessedUpdates;
        private static int _maxRunningCommands = 10;
        private static int _countRunningCommands;
        private static readonly IList<string> RunningCommands = new List<string>();

        public CoreSprintTelegramBot(CoreSprintFactory sprintFactory)
        {
            _sprintFactory = sprintFactory;

            if (!TelegramConfiguration.HasConfiguration())
                TelegramConfiguration.Configure();

            var telegramBotToken = TelegramConfiguration.GetConfiguration()["botToken"];

            _telegramBot = new TelegramBot(telegramBotToken);
            _unprocessedUpdates = _unprocessedUpdates ?? new List<Update>();
        }

        public void Execute()
        {
            //NetTelegramBotApi.Requests
            var updates = GetUpdates().AsParallel().AsOrdered();
            var telegramCommands = GetCommands();

            if (updates.Any())
            {
                SetLastUpdateId(updates.Max(u => u.UpdateId));

                updates =
                    updates.Where(
                        u => telegramCommands.Any(c => u.Message.Text.Trim().StartsWith($"/{c.Name.Trim().ToLower()}")));

                if (updates.Any())
                {
                    foreach (var update in updates)
                    {
                        if (CheckIfOccupied(telegramCommands, update))
                            SayOccupied(update);
                        else
                            ExecuteInNewThread(ExecuteCommands(update));
                    }
                }
            }
        }

        private IEnumerable<ITelegramCommand> GetCommands()
        {
            /*
             report - Relatório do sprint atual com horas trabalhadas e pendentes por profissional
             update_report - Atualiza planilha do sprint atual com as informações do quadro de Sprint do Trello
             update_cards_report - Atualiza lista de cartões do Trello na planilha do sprint atual
             update_work_extract - Atualiza a planilha de horas trabalhadas com o extrato do sprint
             card_info - Recupera informações de estimativa e tempo trabalhado do cartão
             card_working - Recupera qual cartão um ou mais profissionais estão trabalhando
             */
            return new List<ITelegramCommand>
            {
                new TelegramCurrentSprintReport(_telegramBot, _sprintFactory, CoreSprintApp.SpreadsheetId),
                new TelegramCurrentSprintUpdate(_telegramBot, _sprintFactory, CoreSprintApp.TrelloBoardId, CoreSprintApp.SpreadsheetId),
                new TelegramListSprintCards(_telegramBot, _sprintFactory, CoreSprintApp.TrelloBoardId, CoreSprintApp.SpreadsheetId),
                new TelegramWorkExtractUpdate(_telegramBot, _sprintFactory, CoreSprintApp.TrelloBoardId, CoreSprintApp.SpreadsheetId),
                new TelegramCardInfo(_telegramBot, _sprintFactory, CoreSprintApp.TrelloBoardId, CoreSprintApp.SpreadsheetId),
                new TelegramWorkingCard(_telegramBot, _sprintFactory, CoreSprintApp.TrelloBoardId, CoreSprintApp.SpreadsheetId)
            };
        }

        private static bool CheckIfOccupied(IEnumerable<ITelegramCommand> telegramCommands, Update update)
        {
            return _countRunningCommands >= _maxRunningCommands ||
                   telegramCommands.Any(
                       c =>
                           !c.AllowParlallelExecution &&
                           update.Message.Text.Trim().StartsWith($"/{c.Name.Trim().ToLower()}"));
        }

        private void SayOccupied(Update update)
        {
            var message = $"No momento estou ocupado para executar o comando \"{update.Message.Text}\". Assim que desocupar aviso.";
            _unprocessedUpdates.Add(update);
            TelegramCommand.SendMessageToChat(_telegramBot, update.Message.Chat.Id, message);
        }

        private Action ExecuteCommands(Update update)
        {
            return () =>
            {
                var messageText = update.Message.Text.ToLower().Trim();
                var commands = GetCommands().Where(c => messageText.StartsWith($"/{c.Name.Trim().ToLower()}"));

                commands.AsParallel().ForAll(command =>
                {
                    var executionId = $"{command.Name}_{DateTime.Now.ToString("yyyyMMddHHmmssffff")}_{new Random().Next(1, 1000000)}";
                    try
                    {
                        RunningCommands.Add(executionId);
                        SayCommandReceived(command, update, update.Message.Text);
                        command.Execute(update.Message);
                    }
                    catch (Exception e)
                    {
                        var msgError = $"Ocorreu um erro ao executar o comando: {e.Message}\r\n{e.StackTrace}";
                        Console.WriteLine(msgError);

                        command.SendToChat(update.Message.Chat.Id, "Ocorreu um erro ao executar o comando!");
                    }
                    finally
                    {
                        RunningCommands.Remove(executionId);
                    }
                });
            };
        }

        private void ExecuteInNewThread(Action action)
        {
            var thread = new Thread(() =>
            {
                _countRunningCommands++;
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Ocorreu um erro!\r\n{0}", e.StackTrace);
                }
                finally
                {
                    _countRunningCommands--;
                    SayIAmFree();
                }
            });
            thread.Start();
        }

        private void SayIAmFree()
        {
            if (_countRunningCommands < _maxRunningCommands && _unprocessedUpdates != null && _unprocessedUpdates.Any())
            {
                var chats = _unprocessedUpdates.Select(u => u.Message.Chat.Id).Distinct().AsParallel();
                chats.ForAll(chatId =>
                {
                    var commandsQueried =
                        _unprocessedUpdates.Where(un => un.Message.Chat.Id == chatId)
                            .Select(un => un.Message.From.FirstName + ": " + un.Message.Text)
                            .Aggregate((text, next) => text + "\r\n" + next);
                    var msgSayFree =
                        string.Format("Já estou disponível para executar pelo menos um dos comandos solicitados:\r\n{0}",
                            commandsQueried);

                    TelegramCommand.SendMessageToChat(_telegramBot, chatId, msgSayFree);
                    _unprocessedUpdates.RemoveAll(un => un.Message.Chat.Id == chatId);
                });
            }
        }

        private static void SayCommandReceived(ITelegramCommand command, Update update, string userCommand)
        {
            command.SendToChat(update.Message.Chat.Id,
                string.Format("{0}, recebi seu comando \"{1}\".\r\nPor favor, aguarde um momento enquanto processo...",
                    update.Message.From.FirstName, userCommand));
        }

        private IEnumerable<Update> GetUpdates()
        {
            return _telegramBot.MakeRequestAsync(new GetUpdates
            {
                Offset = GetLastUpdateId() + 1
            }).Result ?? new Update[0];
        }

        private static void SetLastUpdateId(long? updateId)
        {
            File.WriteAllText(CoreSprintApp.TelegramDataPath, updateId.ToString());
        }

        private static long GetLastUpdateId()
        {
            var updateId = 1L;
            if (File.Exists(CoreSprintApp.TelegramDataPath))
            {
                var lastUpdateId = File.ReadAllText(CoreSprintApp.TelegramDataPath);
                updateId = string.IsNullOrWhiteSpace(lastUpdateId) ? 1 : long.Parse(lastUpdateId);
            }
            return updateId;
        }
    }
}
