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
            Console.WriteLine("Iniciado com os argumentos: {0}", string.Join(" ", args));

            Execute(args);
        }

        private static void Execute(string[] args)
        {
            const int seconds = 2;
            const int miliseconds = 1000;

            if (args.ToList().Contains("--nostop"))
            {
                while (true)
                {
                    ExecuteCommands(GetCommandList(CoreSprintApp.TrelloBoardId, CoreSprintApp.SpreadsheetId, args));
                    Thread.Sleep(seconds * miliseconds);
                }
            }

            ExecuteCommands(GetCommandList(CoreSprintApp.TrelloBoardId, CoreSprintApp.SpreadsheetId, args));
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

            if (args.ToList().Contains("WorkExtract"))
                commandList.Add(new WorkExtract(sprintFactory, trelloBoardId, spreadsheetId));

            return commandList;
        }


        private static void ExecuteCommands(List<ICommand> commandList)
        {
            CoreSprintApp.ConfigureRemoteIntegrations();

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
