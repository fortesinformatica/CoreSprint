using System;
using System.Collections.Generic;
using System.Globalization;
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
        private static readonly List<string> RunningCommands = new List<string>();

        public CoreSprintTelegramBot(CoreSprintFactory sprintFactory)
        {
            _sprintFactory = sprintFactory;

            if (!TelegramConfiguration.HasConfiguration())
                TelegramConfiguration.Configure();

            var telegramBotToken = TelegramConfiguration.GetConfiguration()["botToken"];

            _telegramBot = new TelegramBot(telegramBotToken);
            _unprocessedUpdates = _unprocessedUpdates ?? new List<Update>();
        }

        private IEnumerable<ITelegramCommand> GetReactiveCommands()
        {
            /*
             report - Relatório do sprint atual com horas trabalhadas e pendentes por profissional
             update_report - Atualiza planilha do sprint atual com as informações do quadro de Sprint do Trello
             update_cards_report - Atualiza lista de cartões do Trello na planilha do sprint atual
             update_work_extract - Atualiza a planilha de horas trabalhadas com o extrato do sprint
             card_info - Recupera informações de estimativa e tempo trabalhado do cartão
             card_working - Recupera qual cartão um ou mais profissionais estão trabalhando
             late - Identifica possíveis atrasos no sprint considerando a data atual e a data final do sprint
             receive_alerts - Registra um chat para receber notificações de alerta
             reassessment - Recupera os cartões que sofreram reestimativa a partir de uma determinada data
             */
            return new List<ITelegramCommand>
            {
                new TelegramRunningSprintReport(_telegramBot, _sprintFactory, CoreSprintApp.SpreadsheetId),
                new TelegramRunningSprintUpdater(_telegramBot, _sprintFactory, CoreSprintApp.TrelloBoardId, CoreSprintApp.SpreadsheetId),
                new TelegramListSprintCards(_telegramBot, _sprintFactory, CoreSprintApp.TrelloBoardId, CoreSprintApp.SpreadsheetId),
                new TelegramWorkExtractUpdate(_telegramBot, _sprintFactory, CoreSprintApp.TrelloBoardId, CoreSprintApp.SpreadsheetId),
                new TelegramCardInfo(_telegramBot, _sprintFactory, CoreSprintApp.TrelloBoardId, CoreSprintApp.SpreadsheetId),
                new TelegramWorkingCard(_telegramBot, _sprintFactory, CoreSprintApp.TrelloBoardId, CoreSprintApp.SpreadsheetId),
                new TelegramLate(_telegramBot, _sprintFactory, CoreSprintApp.TrelloBoardId, CoreSprintApp.SpreadsheetId),
                new TelegramReceiveAlerts(_telegramBot),
                new TelegramReassessment(_telegramBot, _sprintFactory, CoreSprintApp.TrelloBoardId, CoreSprintApp.SpreadsheetId)
            };
        }

        public void Execute()
        {
            ExecuteProactiveCommands();
            ExecuteReactiveCommands();
        }

        private void ExecuteProactiveCommands()
        {
            var strDateTime = File.Exists(CoreSprintApp.TelegramProactivePath) ? File.ReadAllText(CoreSprintApp.TelegramProactivePath) : "";
            var dateTimeNow = DateTime.Now;
            var inTime = (string.IsNullOrWhiteSpace(strDateTime) ||
                          dateTimeNow >
                          Convert.ToDateTime(strDateTime, new CultureInfo("pt-BR", false).DateTimeFormat)
                              .AddHours(3))
                         && dateTimeNow.Hour >= 7 && dateTimeNow.Hour <= 19;

            if (inTime && File.Exists(CoreSprintApp.TelegramChatsPath))
            {
                File.WriteAllText(CoreSprintApp.TelegramProactivePath, dateTimeNow.ToString("dd/MM/yyyy HH:mm:ss"));

                var commands = GetProactiveCommands();
                var chats = File.ReadAllLines(CoreSprintApp.TelegramChatsPath).Select(long.Parse);

                foreach (var command in commands)
                {
                    if (command is TelegramReassessment) //TODO: ver melhor forma de fazer esses tratamentos específicos dos comandos
                        ExecuteInNewThread(() => command.Execute(chats, strDateTime));
                    else
                        ExecuteInNewThread(() => command.Execute(chats));
                }
            }
        }

        private List<ITelegramProactiveCommand> GetProactiveCommands()
        {
            return new List<ITelegramProactiveCommand>
            {
                new TelegramLate(_telegramBot, _sprintFactory, CoreSprintApp.TrelloBoardId, CoreSprintApp.SpreadsheetId),
                new TelegramReassessment(_telegramBot, _sprintFactory, CoreSprintApp.TrelloBoardId, CoreSprintApp.SpreadsheetId)
            };
        }

        private void ExecuteReactiveCommands()
        {
            var telegramCommands = GetReactiveCommands();
            var updates = GetUpdates();

            var enumerableUpdates = updates as Update[] ?? updates.ToArray();
            if (enumerableUpdates.Any()) //TODO: diminuir complexidade ciclomática
            {
                SetLastUpdateId(enumerableUpdates.Max(u => u.UpdateId));
                updates =
                    enumerableUpdates.Where(
                        u => telegramCommands.Any(c => u.Message.Text.Trim().StartsWith($"/{c.Name.Trim().ToLower()}")));
                enumerableUpdates = updates as Update[] ?? updates.ToArray();

                if (enumerableUpdates.Any())
                {
                    var enumerableTelegramCommands = telegramCommands as ITelegramCommand[] ?? telegramCommands.ToArray();

                    foreach (var update in enumerableUpdates)
                    {
                        var isOccupied = CheckIfOccupied(enumerableTelegramCommands, update);

                        if (!isOccupied)
                        {
                            MarkAsRunning(update);
                            ExecuteInNewThread(() => ExecuteCommands(enumerableTelegramCommands, update));
                        }
                        else
                        {
                            SayOccupied(update);
                        }
                    }
                }
            }
        }

        private void MarkAsRunning(Update update)
        {
            var messageText = update.Message.Text.Trim().ToLower();
            var messageTimeId = update.Message.Date.ToString("yyyyMMddHHmmssffff");
            RunningCommands.Add($"{messageText}_{messageTimeId}");
            SayCommandReceived(update);
        }

        private static bool CheckIfOccupied(IEnumerable<ITelegramCommand> telegramCommands, Update update)
        {
            return _countRunningCommands >= _maxRunningCommands ||
                   telegramCommands.Any(
                       c =>
                       {
                           var commandName = c.Name.Trim().ToLower();
                           var updateMessage = update.Message.Text.Trim();
                           return !c.AllowParlallelExecution && updateMessage.StartsWith($"/{commandName}") && RunningCommands.Any(r => r.Contains(commandName));
                       });
        }

        private void SayOccupied(Update update)
        {
            var message = $"No momento estou ocupado para executar o comando \"{update.Message.Text}\". Assim que desocupar aviso.";
            _unprocessedUpdates.Add(update);
            TelegramCommand.SendMessageToChat(_telegramBot, update.Message.Chat.Id, message);
        }

        private void ExecuteCommands(IEnumerable<ITelegramCommand> telegramCommands, Update update)
        {
            var commands = GetCommandsFromUpdate(telegramCommands, update);

            commands.AsParallel().ForAll(command =>
            {
                try
                {
                    command.Execute(update.Message);
                }
                catch (Exception e)
                {
                    var msgError = $"Ocorreu um erro ao executar o comando: {e.Message}\r\n{e.StackTrace}";
                    Console.WriteLine(msgError);

                    command.SendToChat(update.Message.Chat.Id, $"Ocorreu um erro ao executar o comando \"/{command.Name}\"!");
                }
                finally
                {
                    RunningCommands.RemoveAll(c => c.Trim().ToLower().StartsWith($"/{command.Name}"));
                }
            });
        }

        private static IEnumerable<ITelegramCommand> GetCommandsFromUpdate(IEnumerable<ITelegramCommand> telegramCommands, Update update)
        {
            var messageText = update.Message.Text.ToLower().Trim();
            var commands = telegramCommands.Where(c => messageText.StartsWith($"/{c.Name.Trim().ToLower()}"));
            return commands;
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
                var chats = _unprocessedUpdates
                        .Where(u => !RunningCommands.Any(c => u.Message.Text.Trim().ToLower().StartsWith($"/{c.Trim().ToLower()}")))
                            .Select(u => u.Message.Chat.Id)
                            .Distinct()
                            .AsParallel();

                chats.ForAll(chatId =>
                {
                    var commandsQueried =
                        _unprocessedUpdates.Where(un => un.Message.Chat.Id == chatId)
                            .Select(un => un.Message.From.FirstName + ": " + un.Message.Text).Distinct()
                            .Aggregate((text, next) => text + "\r\n" + next);
                    var msgSayFree = $"Já estou disponível para executar pelo menos um dos comandos solicitados:\r\n{commandsQueried}";

                    TelegramCommand.SendMessageToChat(_telegramBot, chatId, msgSayFree);
                    _unprocessedUpdates.RemoveAll(un => un.Message.Chat.Id == chatId);
                });
            }
        }

        private void SayCommandReceived(Update update)
        {
            var message = $"{update.Message.From.FirstName}, recebi seu comando \"{update.Message.Text}\".\r\nPor favor, aguarde um momento enquanto processo...";
            TelegramCommand.SendMessageToChat(_telegramBot, update.Message.Chat.Id, message);
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
