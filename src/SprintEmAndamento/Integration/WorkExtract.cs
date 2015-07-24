using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CoreSprint.Extensions;
using CoreSprint.Factory;
using CoreSprint.Helpers;
using CoreSprint.Models;
using CoreSprint.Spreadsheet;
using CoreSprint.Trello;
using Google.GData.Spreadsheets;
using TrelloNet;

namespace CoreSprint.Integration
{
    public class WorkExtract : ICommand
    {
        private readonly string _trelloBoardId;
        private readonly string _spreadsheetId;
        private readonly ITrelloFacade _trelloFacade;
        private readonly ICardHelper _cardHelper;
        private readonly ISpreadsheetFacade _spreadsheetFacade;
        private readonly IWorksheetHelper _worksheetHelper;
        private readonly ISprintRunningHelper _sprintRunningHelper;

        public WorkExtract(ICoreSprintFactory sprintFactory, string trelloBoardId, string spreadsheetId)
        {
            _trelloBoardId = trelloBoardId;
            _spreadsheetId = spreadsheetId;
            _trelloFacade = sprintFactory.GetTrelloFacade();
            _spreadsheetFacade = sprintFactory.GetSpreadsheetFacade();
            _cardHelper = sprintFactory.GetCardHelper();
            _worksheetHelper = sprintFactory.GetWorksheetHelper();
            _sprintRunningHelper = sprintFactory.GetSprintRunningHelper();
        }

        public void Execute()
        {
            var cards = ExecutionHelper.ExecuteAndRetryOnFail(() => _trelloFacade.GetCards(_trelloBoardId));
            var sprintWorksheet = ExecutionHelper.ExecuteAndRetryOnFail(() => _spreadsheetFacade.GetWorksheet(_spreadsheetId, "SprintCorrente"));
            var sprintPeriod = ExecutionHelper.ExecuteAndRetryOnFail(() => _sprintRunningHelper.GetSprintPeriod(sprintWorksheet));
            var startDate = sprintPeriod["startDate"];
            var endDate = sprintPeriod["endDate"];
            var i = 0;
            IEnumerable<CardWorkDto> allWork = new List<CardWorkDto>();

            var worksheet = ExecutionHelper.ExecuteAndRetryOnFail(() => _worksheetHelper.RedoWorksheet(_spreadsheetId, "HorasTrabalhadas", GetHeadersName()));
            var enumerable = cards as IList<Card> ?? cards.ToList();
            var count = enumerable.Count();

            allWork =
                enumerable.AsParallel().AsOrdered().Aggregate(allWork,
                    (current, card) =>
                    {
                        Console.WriteLine("Analisando cartão ({0}/{1}) {2}", ++i, count, card.Name);
                        return current.Concat(ExecutionHelper.ExecuteAndRetryOnFail(() => _cardHelper.GetCardWorkExtract(card, startDate, endDate)));
                    })
                    .OrderBy(w => w.Professional)
                    .ThenBy(w => w.WorkAt)
                    .ToList();

            count = allWork.Count();
            i = 0;

            foreach (var work in allWork.AsParallel().AsOrdered())
            {
                Console.WriteLine("Inserindo registro de trabalho ({0}/{1})", ++i, count);

                var row = MountWorksheetRow(work);

                //TODO: substituir para inserir em lote
                ExecutionHelper.ExecuteAndRetryOnFail(() => _spreadsheetFacade.InsertInWorksheet(worksheet, row));
            }
        }

        private static ListEntry MountWorksheetRow(CardWorkDto cardWork)
        {
            var cultureInfoPtBr = new CultureInfo("pt-BR", false);
            var row = new ListEntry();

            row.Elements.Add(new ListEntry.Custom { LocalName = "profissional", Value = cardWork.Professional });
            row.Elements.Add(new ListEntry.Custom { LocalName = "cartao", Value = cardWork.CardName });
            row.Elements.Add(new ListEntry.Custom { LocalName = "linkcartao", Value = cardWork.CardLink });
            row.Elements.Add(new ListEntry.Custom { LocalName = "datacomentario", Value = cardWork.CommentAt.ToHumanReadable() });
            row.Elements.Add(new ListEntry.Custom { LocalName = "datahoratrabalho", Value = cardWork.WorkAt.ToHumanReadable() });
            row.Elements.Add(new ListEntry.Custom { LocalName = "trabalhado", Value = cardWork.Worked.ToString(cultureInfoPtBr) });
            row.Elements.Add(new ListEntry.Custom { LocalName = "comentario", Value = cardWork.Comment });

            return row;
        }

        private static List<string> GetHeadersName()
        {
            return new List<string>
            {
                "profissional",
                "cartao",
                "linkcartao",
                "datacomentario",
                "datahoratrabalho",
                "trabalhado",
                "comentario"
            };
        }
    }
}