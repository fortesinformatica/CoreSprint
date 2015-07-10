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

            if (args.ToList().Contains("CurrentSprint"))
                commandList.Add(new CurrentSprint(sprintFactory, trelloBoardId, spreadsheetId));
            
            if (args.ToList().Contains("ListSprintCards"))
                commandList.Add(new ListSprintCards(sprintFactory, trelloBoardId, spreadsheetId));
            
            if (args.ToList().Contains("TelegramAlerts"))
                commandList.Add(new TelegramAlerts(sprintFactory));

            return commandList;
        }

        private static void Execute(List<ICommand> commandList, IEnumerable<string> args)
        {
            const int minutes = 5;
            if (args.ToList().Contains("--nostop"))
            {
                while (true)
                {
                    ExecuteCommands(commandList);
                    Thread.Sleep(minutes * 1000);
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
