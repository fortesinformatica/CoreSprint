using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using CoreSprint.CoreSpreadsheet;
using CoreSprint.CoreTrello;
using CoreSprint.Helpers;
using Google.GData.Spreadsheets;
using TrelloNet;

namespace CoreSprint.Integration
{
    public class ListSprintCards : ICommand
    {
        private readonly string _trelloBoardId;
        private readonly string _spreadsheetId;
        private readonly ITrelloFacade _trelloFacade;
        private readonly ISpreadsheetFacade _spreadsheetFacade;
        private ICardHelper _cardHelper;

        public ListSprintCards(ICoreSprintFactory coreSprintFactory, string trelloBoardId, string spreadsheetId)
        {
            _trelloBoardId = trelloBoardId;
            _spreadsheetId = spreadsheetId;
            _trelloFacade = coreSprintFactory.GetTrelloFacade();
            _spreadsheetFacade = coreSprintFactory.GetSpreadsheetFacade();
            _cardHelper = coreSprintFactory.GetCardHelper();
        }

        public void Execute()
        {
            const string worksheetName = "ListaDeCartoes";
            var worksheet = RedoWorksheet(worksheetName);
            CopyCardsToSpreadsheet(worksheet);
        }

        private void CopyCardsToSpreadsheet(WorksheetEntry worksheet)
        {
            var cards = _trelloFacade.GetCards(_trelloBoardId);
            foreach (var card in cards)
            {
                Console.WriteLine("Inserindo cartão: {0}", card.Name);

                var row = MountWorksheetRow(card);

                //TODO: substituir para inserir em lote
                _spreadsheetFacade.InsertInWorksheet(worksheet, row);
            }
        }

        private WorksheetEntry RedoWorksheet(string worksheetName)
        {
            Console.WriteLine("Recriando aba {0}...", worksheetName);

            var spreadsheet = _spreadsheetFacade.GetSpreadsheet(_spreadsheetId);
            var createdTempWorksheet = false;
            var cellHeaders = GetHeadersName();

            if (spreadsheet.Worksheets.Entries.Count <= 1)
            {
                _spreadsheetFacade.CreateWorksheet(spreadsheet, "Temp", 1, 1);
                createdTempWorksheet = true;
            }

            _spreadsheetFacade.DeleteWorksheet(spreadsheet, worksheetName); //TODO: fazer backup
            _spreadsheetFacade.CreateWorksheet(spreadsheet, worksheetName, 1, (uint)cellHeaders.Count);

            if (createdTempWorksheet)
                _spreadsheetFacade.DeleteWorksheet(spreadsheet, "Temp");

            var worksheet = _spreadsheetFacade.GetWorksheet(spreadsheet, worksheetName);
            _spreadsheetFacade.CreateHeader(worksheet, cellHeaders);
            return worksheet;
        }

        private static List<string> GetHeadersName()
        {
            return new List<string> { "status", "titulo", "responsaveis", "importancia", "urgencia", "estimativa", "trabalhado", "restante", "reestimativa", "rotulos", "link" };
        }

        private ListEntry MountWorksheetRow(Card card)
        {
            var row = new ListEntry();
            var title = _cardHelper.GetCardTitle(card);
            var priority = _cardHelper.GetCardPriority(card);
            var importance = _cardHelper.GetImportance(priority);
            var urgency = _cardHelper.GetUrgency(priority);
            var estimate = _cardHelper.GetCardEstimate(card);
            var labels = _cardHelper.GetCardLabels(card);
            var status = _cardHelper.GetStatus(card);
            var responsible = _cardHelper.GetResponsible(card);
            var workedAndRemainder = _cardHelper.GetWorkedAndRemainder(card);
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
    }
}
