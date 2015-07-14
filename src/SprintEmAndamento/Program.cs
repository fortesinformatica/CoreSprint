using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CoreSprint.Factory;
using CoreSprint.Integration;

namespace CoreSprint
{
    public class Program 
    {
        static void Main(string[] args)
        {
            var commandList = GetCommandList(CoreSprintApp.TrelloBoardId, CoreSprintApp.SpreadsheetId, args);

            Execute(commandList, args);
        }

        private static List<ICommand> GetCommandList(string trelloBoardId, string spreadsheetId, string[] args)
        {
            var sprintFactory = new CoreSprintFactory();
            var commandList = new List<ICommand>();

            if (args.ToList().Contains("CurrentSprintUpdate"))
                commandList.Add(new CurrentSprintUpdate(sprintFactory, trelloBoardId, spreadsheetId));
            
            if (args.ToList().Contains("ListSprintCards"))
                commandList.Add(new ListSprintCards(sprintFactory, trelloBoardId, spreadsheetId));
            
            if (args.ToList().Contains("TelegramBot"))
                commandList.Add(new CoreSprintTelegramBot(sprintFactory));

            return commandList;
        }

        private static void Execute(List<ICommand> commandList, IEnumerable<string> args)
        {
#if DEBUG //TODO: transformar em parâmetros do linha de comando
            const int seconds = 0;
            const int miliseconds = 0;
#else
            const int seconds = 2;
            const int miliseconds = 1000;
#endif
            if (args.ToList().Contains("--nostop"))
            {
                while (true)
                {
                    ExecuteCommands(commandList);
                    Thread.Sleep(seconds * miliseconds);
                }
            }

            ExecuteCommands(commandList);
        }

        private static void ExecuteCommands(List<ICommand> commandList)
        {
            commandList.ForEach(c =>
            {
                try
                {
                    c.Execute();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Erro: {0}", e.Message);
                }
            });
        }
    }
}
