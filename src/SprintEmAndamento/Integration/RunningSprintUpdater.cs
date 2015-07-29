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
    public class RunningSprintUpdater : ICommand
    {
        private readonly string _trelloBoardId;
        private readonly string _spreadsheetId;
        private readonly ITrelloFacade _trelloFacade;
        private readonly ISpreadsheetFacade _spreadsheetFacade;
        private readonly ICardHelper _cardHelper;
        private WorksheetEntry _worksheet;
        private readonly ISprintRunningHelper _sprintRunningHelper;

        public RunningSprintUpdater(ICoreSprintFactory coreSprintFactory, string trelloBoardId, string spreadsheetId)
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

            var cards = ExecutionHelper.ExecuteAndRetryOnFail(() => _trelloFacade.GetCards(_trelloBoardId));

            //recupera variáveis
            _worksheet = ExecutionHelper.ExecuteAndRetryOnFail(() => _spreadsheetFacade.GetWorksheet(_spreadsheetId, worksheetName));

            var sprintPeriod = ExecutionHelper.ExecuteAndRetryOnFail(() => _sprintRunningHelper.GetSprintPeriod(_worksheet));
            var startDate = sprintPeriod["startDate"];
            var endDate = sprintPeriod["endDate"];

            var firstColumn = ExecutionHelper.ExecuteAndRetryOnFail(() => _spreadsheetFacade.GetCellsValues(_worksheet, 1, uint.MaxValue, 1, 1)).ToList();
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
            var columnPosition = ExecutionHelper.ExecuteAndRetryOnFail(() => _sprintRunningHelper.GetHeaderColumnPosition(_worksheet, sprintPlanningPos, "Tempo alocado"));
            SaveRunningSprintData(resultOfAnalysis["allocationsByResponsible"], sprintPlanningPos, columnPosition); //TODO: utilizar constante

            Console.WriteLine("\t> Atualizando tempo operacional pendente...");
            columnPosition = ExecutionHelper.ExecuteAndRetryOnFail(() => _sprintRunningHelper.GetHeaderColumnPosition(_worksheet, sprintRunningPos, "Trabalho alocado pendente"));
            SaveRunningSprintData(resultOfAnalysis["pendingByResponsible"], sprintRunningPos, columnPosition); //TODO: utilizar constante

            Console.WriteLine("\t> Atualizando tempo total trabalhado no sprint...");
            columnPosition = ExecutionHelper.ExecuteAndRetryOnFail(() => _sprintRunningHelper.GetHeaderColumnPosition(_worksheet, sprintRunningPos, "Total trabalhado"));
            SaveRunningSprintData(resultOfAnalysis["totalWorked"], sprintRunningPos, columnPosition); //TODO: utilizar constante

            Console.WriteLine("\t> Atualizando tempo trabalhado no sprint para alocações...");
            columnPosition = ExecutionHelper.ExecuteAndRetryOnFail(() => _sprintRunningHelper.GetHeaderColumnPosition(_worksheet, sprintRunningPos, "Trabalhado em cartões alocados"));
            SaveRunningSprintData(resultOfAnalysis["workedOnAllocations"], sprintRunningPos, columnPosition); //TODO: utilizar constante

            Console.WriteLine("\t> Atualizando alocações por rótulo...");
            columnPosition = ExecutionHelper.ExecuteAndRetryOnFail(() => _sprintRunningHelper.GetHeaderColumnPosition(_worksheet, sprintAllocationByLabelsPos, "Tempo alocado"));
            SaveRunningSprintData(resultOfAnalysis["allocationByLabels"], sprintAllocationByLabelsPos, columnPosition); //TODO: utilizar constante
        }

        private void SaveRunningSprintData(Dictionary<string, double> resultOfAnalysis, Dictionary<string, uint> sectionPositions, uint columnPosition)
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
            var pendingByResponsible = new Dictionary<string, double>();
            var workedOnAllocations = new Dictionary<string, double>();
            var totalWorked = new Dictionary<string, double>();
            var allocationByLabels = new Dictionary<string, double>();

            foreach (var card in enumerableCards)
            {
                Console.WriteLine("\t> ({0}/{1}) Cartão: {2}", ++i, count, card.Name);

                var card1 = card;
                var responsibles = ExecutionHelper.ExecuteAndRetryOnFail(() => _cardHelper.GetResponsible(card1)).Trim().Replace("-", "--Indefinido--");
                var estimate = _cardHelper.GetCardEstimate(card);
                var comments = ExecutionHelper.ExecuteAndRetryOnFail(() => _cardHelper.GetCardComments(card1)).ToList();
                var labels = _cardHelper.GetCardLabels(card);

                var beforeRunning = _cardHelper.GetWorkedAndPending(estimate, comments, startDate);
                var running = _cardHelper.GetWorkedAndPending(estimate, comments, endDate);

                foreach (var responsible in responsibles.Split(';').AsParallel())
                {
                    var runningByResponsible = _cardHelper.GetWorkedAndPending(estimate, comments, responsible, startDate, endDate);

                    Calculate(allocationsByResponsible, responsible, beforeRunning["pending"]);
                    Calculate(pendingByResponsible, responsible, running["pending"]);
                    Calculate(workedOnAllocations, responsible, runningByResponsible["worked"]);
                }

                foreach (var boardMember in boardMembers.AsParallel())
                {
                    var onAllocations = _cardHelper.GetWorkedAndPending(estimate, comments, boardMember.FullName, startDate, endDate);
                    Calculate(totalWorked, boardMember.FullName, onAllocations["worked"]);
                }

                foreach (var label in labels.Split(';').AsParallel())
                {
                    Calculate(allocationByLabels, label, beforeRunning["pending"]);
                }
            }

            result.Add("allocationsByResponsible", allocationsByResponsible);
            result.Add("pendingByResponsible", pendingByResponsible);
            result.Add("workedOnAllocations", workedOnAllocations);
            result.Add("totalWorked", totalWorked);
            result.Add("allocationByLabels", allocationByLabels);

            return result;
        }

        private static void Calculate(IDictionary<string, double> information, string label, double value)
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
                ExecutionHelper.ExecuteAndRetryOnFail(
                    () =>
                        _spreadsheetFacade.SaveToCell(worksheet, index, columnPosition,
                            value.ToString(CultureInfo.InvariantCulture).Replace(".", ",")));
        }
    }
}