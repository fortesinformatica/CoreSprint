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
    public class ListSprintCards : ICommand
    {
        private readonly string _trelloBoardId;
        private readonly string _spreadsheetId;
        private readonly ITrelloFacade _trelloFacade;
        private readonly ISpreadsheetFacade _spreadsheetFacade;
        private readonly ICardHelper _cardHelper;
        private readonly IWorksheetHelper _worksheetHelper;

        public ListSprintCards(ICoreSprintFactory coreSprintFactory, string trelloBoardId, string spreadsheetId)
        {
            _trelloBoardId = trelloBoardId;
            _spreadsheetId = spreadsheetId;
            _trelloFacade = coreSprintFactory.GetTrelloFacade();
            _spreadsheetFacade = coreSprintFactory.GetSpreadsheetFacade();
            _cardHelper = coreSprintFactory.GetCardHelper();
            _worksheetHelper = coreSprintFactory.GetWorksheetHelper();
        }

        public void Execute()
        {
            const string worksheetName = "ListaDeCartoes";

            var worksheet = _worksheetHelper.RedoWorksheet(_spreadsheetId, worksheetName, GetHeadersName());
            CopyCardsToSpreadsheet(worksheet);
        }

        private void CopyCardsToSpreadsheet(WorksheetEntry worksheet)
        {
            var cards = _trelloFacade.GetCards(_trelloBoardId).ToList();
            var i = 0;
            var count = cards.Count();

            cards.AsParallel().ForAll(card =>
            {
                Console.WriteLine("Inserindo cartão ({0}/{1}): {2}", ++i, count, card.Name);

                var row = MountWorksheetRow(card);

                //TODO: substituir para inserir em lote
                _spreadsheetFacade.InsertInWorksheet(worksheet, row);
            });

            //foreach (var card in cards)
            //{
            //    Console.WriteLine("Inserindo cartão ({0}/{1}): {2}", ++i, count, card.Name);

            //    var row = MountWorksheetRow(card);

            //    //TODO: substituir para inserir em lote
            //    _spreadsheetFacade.InsertInWorksheet(worksheet, row);
            //}
        }

        private static List<string> GetHeadersName()
        {
            return new List<string>
            {
                "status",
                "titulo",
                "responsaveis",
                "importancia",
                "urgencia",
                "estimativa",
                "trabalhado",
                "restante",
                "reestimativa",
                "rotulos",
                "link"
            };
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
