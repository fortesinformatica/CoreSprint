using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using CoreSprint.CoreSpreadsheet;
using CoreSprint.CoreTrello;
using Google.GData.Spreadsheets;
using TrelloNet;

namespace CoreSprint.Integration
{
    public class ListSprintCards : ICommand
    {
        private readonly string _trelloBoardId;
        private readonly string _spreadsheetId;
        private readonly TrelloSprint _trelloSprint;
        private readonly SpreadsheetSprint _spreadsheetSprint;
        private readonly string _strNumberPattern;
        private readonly string _nothingValue;
        private readonly string _delimiter;
        private readonly string _strPriorityPattern;
        private readonly string _strEstimatePattern;

        public ListSprintCards(SprintFactory sprintFactory, string trelloBoardId, string spreadsheetId)
        {
            _trelloBoardId = trelloBoardId;
            _spreadsheetId = spreadsheetId;
            _trelloSprint = sprintFactory.GetTrelloSprint();
            _spreadsheetSprint = sprintFactory.GetSpreadsheetSprint();
            _strNumberPattern = @"[0-9]+[\.,]?[0-9]*";
            _nothingValue = "-";
            _delimiter = ";";
            _strPriorityPattern = @"\[i[0-9]+-u[0-9]+\]";
            _strEstimatePattern = string.Format(@"\{{(\s)*({0})(\s)*hora[\sa-zA-Z]*\}}", _strNumberPattern);
        }

        public void Execute()
        {
            const string worksheetName = "ListaDeCartoes";
            var worksheet = RedoWorksheet(worksheetName);
            CopyCardsToSpreadsheet(worksheet);
        }

        private void CopyCardsToSpreadsheet(WorksheetEntry worksheet)
        {
            var cards = _trelloSprint.GetCards(_trelloBoardId);
            foreach (var card in cards)
            {
                Console.WriteLine("Inserindo cartão: {0}", card.Name);

                var row = MountWorksheetRow(card);

                //TODO: substituir para inserir em lote
                _spreadsheetSprint.InsertInWorksheet(worksheet, row);
            }
        }

        private WorksheetEntry RedoWorksheet(string worksheetName)
        {
            Console.WriteLine("Recriando aba {0}...", worksheetName);

            var spreadsheet = _spreadsheetSprint.GetSpreadsheet(_spreadsheetId);
            var createdTempWorksheet = false;
            var cellHeaders = GetHeadersName();

            if (spreadsheet.Worksheets.Entries.Count <= 1)
            {
                _spreadsheetSprint.CreateWorksheet(spreadsheet, "Temp", 1, 1);
                createdTempWorksheet = true;
            }

            _spreadsheetSprint.DeleteWorksheet(spreadsheet, worksheetName); //TODO: fazer backup
            _spreadsheetSprint.CreateWorksheet(spreadsheet, worksheetName, 1, (uint)cellHeaders.Count);

            if (createdTempWorksheet)
                _spreadsheetSprint.DeleteWorksheet(spreadsheet, "Temp");

            var worksheet = _spreadsheetSprint.GetWorksheet(spreadsheet, worksheetName);
            _spreadsheetSprint.CreateHeader(worksheet, cellHeaders);
            return worksheet;
        }

        private static List<string> GetHeadersName()
        {
            return new List<string> { "status", "titulo", "responsaveis", "importancia", "urgencia", "estimativa", "trabalhado", "restante", "reestimativa", "rotulos", "link" };
        }

        private ListEntry MountWorksheetRow(Card card)
        {
            var row = new ListEntry();
            var title = GetCardTitle(card);
            var priority = GetCardPriority(card);
            var importance = GetImportance(priority);
            var urgency = GetUrgency(priority);
            var estimate = GetCardEstimate(card);
            var labels = GetCardLabels(card);
            var status = GetStatus(card);
            var responsible = GetResponsible(card);
            var workedAndRemainder = GetWorkedAndRemainder(card);
            var worked = workedAndRemainder["worked"].ToString(CultureInfo.InvariantCulture).Replace(".", ",");
            var remainder = workedAndRemainder["remainder"].ToString(CultureInfo.InvariantCulture).Replace(".", ",");
            var reassessment = (workedAndRemainder["worked"] + workedAndRemainder["remainder"]).ToString(CultureInfo.InvariantCulture).Replace(".", ",");

            row.Elements.Add(new ListEntry.Custom { LocalName = "status", Value = status });
            row.Elements.Add(new ListEntry.Custom { LocalName = "titulo", Value = title });
            row.Elements.Add(new ListEntry.Custom { LocalName = "responsaveis", Value = responsible });
            row.Elements.Add(new ListEntry.Custom { LocalName = "importancia", Value = importance });
            row.Elements.Add(new ListEntry.Custom { LocalName = "urgencia", Value = urgency });
            row.Elements.Add(new ListEntry.Custom { LocalName = "estimativa", Value = estimate });
            row.Elements.Add(new ListEntry.Custom { LocalName = "trabalhado", Value = worked });
            row.Elements.Add(new ListEntry.Custom { LocalName = "restante", Value = remainder });
            row.Elements.Add(new ListEntry.Custom { LocalName = "reestimativa", Value = reassessment });
            row.Elements.Add(new ListEntry.Custom { LocalName = "rotulos", Value = labels });
            row.Elements.Add(new ListEntry.Custom { LocalName = "link", Value = card.ShortUrl });

            return row;
        }

        //TODO: criar card helpers
        private string GetUrgency(string priority)
        {
            var pattern = new Regex(@"u[0-9]+", RegexOptions.IgnoreCase);
            var patternNumber = new Regex(_strNumberPattern);
            var matchUrgency = pattern.Match(priority);
            return matchUrgency.Success ? patternNumber.Match(matchUrgency.Value).Value : "0";
        }

        private string GetImportance(string priority)
        {
            var pattern = new Regex(@"i[0-9]+", RegexOptions.IgnoreCase);
            var patternNumber = new Regex(_strNumberPattern);
            var matchUrgency = pattern.Match(priority);
            return matchUrgency.Success ? patternNumber.Match(matchUrgency.Value).Value : "0";
        }
        private string GetCardTitle(Card card)
        {
            var replacedPriority = Regex.Replace(card.Name, _strPriorityPattern, "", RegexOptions.IgnoreCase);
            return Regex.Replace(replacedPriority, _strEstimatePattern, "", RegexOptions.IgnoreCase).Trim();
        }

        private Dictionary<string, double> GetWorkedAndRemainder(Card card)
        {
            var comments = _trelloSprint.GetActions(card, new[] { ActionType.CommentCard }).OfType<CommentCardAction>().Reverse();
            var numberPattern = new Regex(_strNumberPattern, RegexOptions.IgnoreCase);
            var workedPattern = new Regex(string.Format(@">(\s)*trabalhado(\s)+{0}(\s)+hora[\sa-zA-Z]*", _strNumberPattern), RegexOptions.IgnoreCase);
            var remainderPattern = new Regex(string.Format(@">(\s)*(restam|restante)(\s)+{0}(\s)+hora[\sa-zA-Z]*", _strNumberPattern), RegexOptions.IgnoreCase);
            var worked = 0D;
            double remainder;
            var cardEstimate = GetCardEstimate(card);

            double.TryParse(numberPattern.Match(cardEstimate).Value, out remainder);

            //TODO: refatorar
            foreach (var comment in comments)
            {
                var matchesWorked = workedPattern.Matches(comment.Data.Text);
                var matchesRemainder = remainderPattern.Matches(comment.Data.Text);

                foreach (Match match in matchesWorked)
                {
                    var matchNumber = numberPattern.Match(match.Value);
                    var workedInComment = double.Parse(matchNumber.Value);
                    worked += matchNumber.Success ? workedInComment : 0D;
                    remainder -= matchesRemainder.Count > 0 ? 0D : workedInComment;
                }

                foreach (Match match in matchesRemainder)
                {
                    var matchNumber = numberPattern.Match(match.Value);
                    remainder = matchNumber.Success ? double.Parse(matchNumber.Value) : 0D;
                }
            }

            return new Dictionary<string, double> { { "worked", worked }, { "remainder", remainder } };
        }
        private string GetResponsible(Card card)
        {
            return card.IdMembers.Any()
                ? _trelloSprint.GetMembers(card).Select(m => m.FullName).Aggregate((i, j) => i + _delimiter + j)
                : _nothingValue;
        }
        private string GetStatus(Card card)
        {
            return _trelloSprint.GetList(card).Name.Split('\n').FirstOrDefault();
        }
        private string GetCardLabels(Card card)
        {
            return card.Labels.Any()
                ? card.Labels.Select(l => l.Name).Aggregate((i, j) => i + _delimiter + j)
                : _nothingValue;
        }

        private string GetCardEstimate(Card card)
        {
            var estimatePattern = new Regex(_strEstimatePattern, RegexOptions.IgnoreCase);
            var matchPattern = estimatePattern.Match(card.Name);
            var numberPattern = new Regex(_strNumberPattern, RegexOptions.IgnoreCase);

            if (matchPattern.Success)
            {
                var numberMatch = numberPattern.Match(matchPattern.Value);
                return numberMatch.Success ? numberMatch.Value.Replace(".", ",") : "0";
            }
            return "0";
        }

        private string GetCardPriority(Card card)
        {
            var pattern = new Regex(_strPriorityPattern, RegexOptions.IgnoreCase);
            var match = pattern.Match(card.Name);
            return match.Success ? match.Value : _nothingValue;
        }
    }
}
