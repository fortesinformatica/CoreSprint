﻿using System;
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
            const int seconds = 2;
            const int miliseconds = 1000;

            if (args.ToList().Contains("--nostop"))
            {
                while (true)
                {
                    CoreSprintApp.ConfigureRemoteIntegrations();
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
