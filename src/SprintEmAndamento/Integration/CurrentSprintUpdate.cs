using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CoreSprint.Factory;
using CoreSprint.Helpers;
using CoreSprint.Spreadsheet;
using CoreSprint.Trello;
using Google.GData.Spreadsheets;
using TrelloNet;

namespace CoreSprint.Integration
{
    public class CurrentSprintUpdate : ICommand
    {
        private readonly string _trelloBoardId;
        private readonly string _spreadsheetId;
        private readonly ITrelloFacade _trelloFacade;
        private readonly ISpreadsheetFacade _spreadsheetFacade;
        private readonly ICardHelper _cardHelper;
        private WorksheetEntry _worksheet;
        private readonly ISprintRunningHelper _sprintRunningHelper;

        public CurrentSprintUpdate(ICoreSprintFactory coreSprintFactory, string trelloBoardId, string spreadsheetId)
        {
            _trelloBoardId = trelloBoardId;
            _spreadsheetId = spreadsheetId;
            _trelloFacade = coreSprintFactory.GetTrelloFacade();
            _spreadsheetFacade = coreSprintFactory.GetSpreadsheetFacade();
            _cardHelper = coreSprintFactory.GetCardHelper();
            _sprintRunningHelper = coreSprintFactory.GetSprintRunningHelper();
        }

        public void Execute()
        {
            const string worksheetName = "SprintCorrente";

            Console.WriteLine("Recuperando alocações dos profissionais...");

            var cards = _trelloFacade.GetCards(_trelloBoardId);

            //recupera variáveis
            _worksheet = _spreadsheetFacade.GetWorksheet(_spreadsheetId, worksheetName);

            var dateFormat = new CultureInfo("pt-BR", false).DateTimeFormat;
            var strStartDate = _spreadsheetFacade.GetCellValue(_worksheet, 2, 2);
            var strEndDate = _spreadsheetFacade.GetCellValue(_worksheet, 3, 2);

            var startDate = Convert.ToDateTime(strStartDate, dateFormat);
            var endDate = Convert.ToDateTime(strEndDate, dateFormat);
            var firstColumn = _spreadsheetFacade.GetCellsValues(_worksheet, 1, uint.MaxValue, 1, 1).ToList();
            var sprintPlanningPos = _sprintRunningHelper.GetSectionLinesPosition(firstColumn, "Relatório de planejamento do sprint");
            var sprintRunningPos = _sprintRunningHelper.GetSectionLinesPosition(firstColumn, "Relatório de andamento do sprint");
            var sprintAllocationByLabelsPos = _sprintRunningHelper.GetSectionLinesPosition(firstColumn, "Alocações por rótulo");
            //

            var resultOfAnalysis = AnalyzeCards(cards, startDate, endDate);
            UpdateSpreadsheet(resultOfAnalysis, sprintPlanningPos, sprintRunningPos, sprintAllocationByLabelsPos);
        }

        private void UpdateSpreadsheet(Dictionary<string, Dictionary<string, double>> resultOfAnalysis, Dictionary<string, uint> sprintPlanningPos,
            Dictionary<string, uint> sprintRunningPos, Dictionary<string, uint> sprintAllocationByLabelsPos)
        {
            Console.WriteLine("Atualizando relatório de planejamento de sprint...");

            Console.WriteLine("\t> Atualizando alocações...");
            SaveCurrentSprintData(resultOfAnalysis["allocationsByResponsible"], sprintPlanningPos,
                _sprintRunningHelper.GetHeaderColumnPosition(_worksheet, sprintPlanningPos, "Tempo alocado")); //TODO: utilizar constante

            Console.WriteLine("\t> Atualizando tempo operacional pendente...");
            SaveCurrentSprintData(resultOfAnalysis["remainderByResponsible"], sprintRunningPos,
                _sprintRunningHelper.GetHeaderColumnPosition(_worksheet, sprintRunningPos, "Trabalho alocado pendente")); //TODO: utilizar constante

            Console.WriteLine("\t> Atualizando tempo total trabalhado no sprint...");
            SaveCurrentSprintData(resultOfAnalysis["totalWorked"], sprintRunningPos,
                _sprintRunningHelper.GetHeaderColumnPosition(_worksheet, sprintRunningPos, "Total trabalhado")); //TODO: utilizar constante

            Console.WriteLine("\t> Atualizando tempo trabalhado no sprint para alocações...");
            SaveCurrentSprintData(resultOfAnalysis["workedOnAllocations"], sprintRunningPos,
                _sprintRunningHelper.GetHeaderColumnPosition(_worksheet, sprintRunningPos, "Trabalhado em cartões alocados")); //TODO: utilizar constante

            Console.WriteLine("\t> Atualizando alocações por rótulo...");
            SaveCurrentSprintData(resultOfAnalysis["allocationByLabels"], sprintAllocationByLabelsPos,
                _sprintRunningHelper.GetHeaderColumnPosition(_worksheet, sprintAllocationByLabelsPos, "Tempo alocado")); //TODO: utilizar constante
        }

        private void SaveCurrentSprintData(Dictionary<string, double> resultOfAnalysis, Dictionary<string, uint> sectionPositions, uint columnPosition)
        {
            if (!resultOfAnalysis.Any(r => r.Key.Equals("--Indefinido--")))
                resultOfAnalysis.Add("--Indefinido--", 0);

            foreach (var keyValue in resultOfAnalysis.AsParallel())
            {
                SetSprintValueByResponsible(_worksheet, sectionPositions, columnPosition, keyValue.Key, keyValue.Value);
                Console.WriteLine("\t\t> {0} - {1}", keyValue.Key, keyValue.Value);
            }
        }

        private Dictionary<string, Dictionary<string, double>> AnalyzeCards(IEnumerable<Card> cards, DateTime startDate, DateTime endDate)
        {
            Console.WriteLine("Analisando cartões...");

            var i = 0;
            var enumerableCards = (cards as IList<Card> ?? cards.ToList()).AsParallel();
            var count = enumerableCards.Count();
            var result = new Dictionary<string, Dictionary<string, double>>();
            var board = _trelloFacade.GetBoard(_trelloBoardId);
            var boardMembers = _trelloFacade.GetMembers(board).ToList();

            var allocationsByResponsible = new Dictionary<string, double>();
            var remainderByResponsible = new Dictionary<string, double>();
            var workedOnAllocations = new Dictionary<string, double>();
            var totalWorked = new Dictionary<string, double>();
            var allocationByLabels = new Dictionary<string, double>();

            foreach (var card in enumerableCards)
            {
                Console.WriteLine("\t> ({0}/{1}) Cartão: {2}", ++i, count, card.Name);

                var responsibles = _cardHelper.GetResponsible(card).Trim().Replace("-", "--Indefinido--");
                var estimate = _cardHelper.GetCardEstimate(card);
                var comments = _cardHelper.GetCardComments(card).ToList();
                var labels = _cardHelper.GetCardLabels(card);

                var beforeRunning = _cardHelper.GetWorkedAndRemainder(estimate, comments, startDate);
                var running = _cardHelper.GetWorkedAndRemainder(estimate, comments, endDate);

                foreach (var responsible in responsibles.Split(';').AsParallel())
                {
                    var runningByResponsible = _cardHelper.GetWorkedAndRemainder(estimate, comments, responsible, startDate, endDate);

                    Calculate(allocationsByResponsible, responsible, beforeRunning["remainder"]);
                    Calculate(remainderByResponsible, responsible, running["remainder"]);
                    Calculate(workedOnAllocations, responsible, runningByResponsible["worked"]);
                }

                foreach (var boardMember in boardMembers.AsParallel())
                {
                    var onAllocations = _cardHelper.GetWorkedAndRemainder(estimate, comments, boardMember.FullName, startDate, endDate);
                    Calculate(totalWorked, boardMember.FullName, onAllocations["worked"]);
                }

                foreach (var label in labels.Split(';').AsParallel())
                {
                    Calculate(allocationByLabels, label, beforeRunning["remainder"]);
                }
            }

            result.Add("allocationsByResponsible", allocationsByResponsible);
            result.Add("remainderByResponsible", remainderByResponsible);
            result.Add("workedOnAllocations", workedOnAllocations);
            result.Add("totalWorked", totalWorked);
            result.Add("allocationByLabels", allocationByLabels);

            return result;
        }

        private static void Calculate(Dictionary<string, double> information, string label, double value)
        {
            if (!information.ContainsKey(label))
                information.Add(label, 0D);
            information[label] += value;
        }

        private void SetSprintValueByResponsible(WorksheetEntry worksheet, Dictionary<string, uint> sprintPlanningPos, uint columnPosition, string label, double value)
        {
            var index =
                sprintPlanningPos.FirstOrDefault(
                    k =>
                        k.Key.ToLower().Contains(label.ToLower()) ||
                        label.ToLower().Contains(k.Key.ToLower())).Value;

            if (index > 0)
                _spreadsheetFacade.SaveToCell(worksheet, index, columnPosition,
                    value.ToString(CultureInfo.InvariantCulture).Replace(".", ","));
        }
    }
}