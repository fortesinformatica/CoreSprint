﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CoreSprint.CoreSpreadsheet;
using CoreSprint.CoreTrello;
using CoreSprint.Helpers;
using Google.GData.Spreadsheets;
using TrelloNet;

namespace CoreSprint.Integration
{
    public class CurrentSprint : ICommand
    {
        private readonly string _trelloBoardId;
        private readonly string _spreadsheetId;
        private readonly ITrelloFacade _trelloFacade;
        private readonly ISpreadsheetFacade _spreadsheetFacade;
        private readonly ICardHelper _cardHelper;

        public CurrentSprint(ICoreSprintFactory coreSprintFactory, string trelloBoardId, string spreadsheetId)
        {
            _trelloBoardId = trelloBoardId;
            _spreadsheetId = spreadsheetId;
            _trelloFacade = coreSprintFactory.GetTrelloFacade();
            _spreadsheetFacade = coreSprintFactory.GetSpreadsheetFacade();
            _cardHelper = coreSprintFactory.GetCardHelper();
        }

        public void Execute()
        {
            Console.WriteLine("Recuperando alocações dos profissionais...");

            const string worksheetName = "SprintCorrente";
            var cards = _trelloFacade.GetCards(_trelloBoardId);

            //recupera variáveis
            var worksheet = _spreadsheetFacade.GetWorksheet(_spreadsheetId, worksheetName);

            var dateFormat = new CultureInfo("pt-BR", false).DateTimeFormat;
            var strStartDate = _spreadsheetFacade.GetCellValue(worksheet, 2, 2);
            var strEndDate = _spreadsheetFacade.GetCellValue(worksheet, 3, 2);

            var startDate = Convert.ToDateTime(strStartDate, dateFormat);
            var endDate = Convert.ToDateTime(strEndDate, dateFormat);
            var firstColumn = _spreadsheetFacade.GetCellValue(worksheet, 1, uint.MaxValue, 1, 1).ToList();
            var sprintPlanningPos = GetSprintPlanningPositions(firstColumn);
            var sprintRunningPos = GetSprintRunningPositions(firstColumn);
            //

            var resultOfAnalysis = AnalyzeCards(cards, startDate, endDate);

            Console.WriteLine("Atualizando relatório de planejamento de sprint...");
            
            Console.WriteLine("\t> Atualizando alocações...");
            SaveCurrentSprintData(worksheet, resultOfAnalysis["allocationsByResponsible"], sprintPlanningPos, 3);

            Console.WriteLine("\t> Atualizando tempo operacional pendente...");
            SaveCurrentSprintData(worksheet, resultOfAnalysis["remainderByResponsible"], sprintRunningPos, 4);

            Console.WriteLine("\t> Atualizando tempo total trabalhado no sprint...");
            SaveCurrentSprintData(worksheet, resultOfAnalysis["totalWorked"], sprintRunningPos, 3);

            Console.WriteLine("\t> Atualizando tempo trabalhado no sprint para alocações...");
            SaveCurrentSprintData(worksheet, resultOfAnalysis["workedOnAllocations"], sprintRunningPos, 5);
        }

        private void SaveCurrentSprintData(WorksheetEntry worksheet, Dictionary<string, double> resultOfAnalysis, Dictionary<string, uint> sectionPositions, uint columnPosition)
        {
            foreach (var keyValue in resultOfAnalysis)
            {
                SetSprintValueByResponsible(worksheet, sectionPositions, columnPosition, keyValue.Key, keyValue.Value);
                Console.WriteLine("\t\t> {0} - {1}", keyValue.Key, keyValue.Value);
            }
        }

        private Dictionary<string, Dictionary<string, double>> AnalyzeCards(IEnumerable<Card> cards, DateTime startDate, DateTime endDate)
        {
            Console.WriteLine("Analisando cartões...");

            var i = 0;
            var enumerableCards = cards as IList<Card> ?? cards.ToList();
            var count = enumerableCards.Count();
            var result = new Dictionary<string, Dictionary<string, double>>();
            var board = _trelloFacade.GetBoard(_trelloBoardId);
            var boardMembers = _trelloFacade.GetMembers(board).ToList();

            var allocationsByResponsible = new Dictionary<string, double>();
            var remainderByResponsible = new Dictionary<string, double>();
            var workedOnAllocations = new Dictionary<string, double>();
            var totalWorked = new Dictionary<string, double>();

            foreach (var card in enumerableCards)
            {
                Console.WriteLine("\t> ({0}/{1}) Cartão: {2}", ++i, count, card.Name);

                var responsibles = _cardHelper.GetResponsible(card).Trim().Replace("-", "--Ninguém--");
                var estimate = _cardHelper.GetCardEstimate(card);
                var comments = _cardHelper.GetCardComments(card).ToList();

                var beforeRunning = _cardHelper.GetWorkedAndRemainder(estimate, comments, startDate);
                var running = _cardHelper.GetWorkedAndRemainder(estimate, comments, endDate);

                foreach (var responsible in responsibles.Split(';'))
                {
                    var runningByResponsible = _cardHelper.GetWorkedAndRemainder(estimate, comments, responsible, startDate, endDate);
                    Calculate(allocationsByResponsible, responsible, beforeRunning["remainder"]);
                    Calculate(remainderByResponsible, responsible, running["remainder"]);
                    Calculate(workedOnAllocations, responsible, runningByResponsible["worked"]);
                }

                foreach (var boardMember in boardMembers)
                {
                    var onAllocations = _cardHelper.GetWorkedAndRemainder(estimate, comments, boardMember.FullName, startDate, endDate);
                    Calculate(totalWorked, boardMember.FullName, onAllocations["worked"]);
                }
            }

            result.Add("allocationsByResponsible", allocationsByResponsible);
            result.Add("remainderByResponsible", remainderByResponsible);
            result.Add("workedOnAllocations", workedOnAllocations);
            result.Add("totalWorked", totalWorked);

            return result;
        }

        private static void Calculate(Dictionary<string, double> information, string responsible, double remainder)
        {
            if (!information.ContainsKey(responsible))
                information.Add(responsible, 0D);
            information[responsible] += remainder;
        }

        private void SetSprintValueByResponsible(WorksheetEntry worksheet, Dictionary<string, uint> sprintPlanningPos, uint columnPosition, string responsible, double estimate)
        {
            var index =
                sprintPlanningPos.FirstOrDefault(
                    k =>
                        k.Key.ToLower().Contains(responsible.ToLower()) ||
                        responsible.ToLower().Contains(k.Key.ToLower())).Value;

            if (index > 0)
                _spreadsheetFacade.SaveToCell(worksheet, index, columnPosition,
                    estimate.ToString(CultureInfo.InvariantCulture).Replace(".", ","));
        }

        private Dictionary<string, uint> GetSprintRunningPositions(IEnumerable<CellEntry> firstColumn)
        {
            var positions = new Dictionary<string, uint>();
            var cellEntries = firstColumn as IList<CellEntry> ?? firstColumn.ToList();

            CalculateSectionPosition(cellEntries, positions,
                c => c.Value.Equals("Relatório de andamento do sprint"),
                c => false);

            return positions;
        }

        private Dictionary<string, uint> GetSprintPlanningPositions(IEnumerable<CellEntry> firstColumn)
        {
            var positions = new Dictionary<string, uint>();
            var cellEntries = firstColumn as IList<CellEntry> ?? firstColumn.ToList();

            CalculateSectionPosition(cellEntries, positions,
                c => c.Value.Equals("Relatório de planejamento do sprint"),
                c => c.Value.Equals("Relatório de andamento do sprint"));

            return positions;
        }

        private void CalculateSectionPosition(IEnumerable<CellEntry> firstColumn, Dictionary<string, uint> positions,
            Func<CellEntry, bool> checkIfEnterInSection, Func<CellEntry, bool> checkIfOutFromSection)
        {
            var inSection = false;

            foreach (var cellEntry in firstColumn)
            {
                if (checkIfEnterInSection(cellEntry))
                {
                    inSection = true;
                    continue;
                }

                if (checkIfOutFromSection(cellEntry))
                    break;

                if (inSection && !string.IsNullOrWhiteSpace(cellEntry.Value))
                    positions.Add(cellEntry.Value.Trim(), cellEntry.Row);
            }
        }
    }
}