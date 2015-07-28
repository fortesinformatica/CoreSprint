using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CoreSprint.Spreadsheet;
using Google.GData.Spreadsheets;

namespace CoreSprint.Helpers
{
    public class SprintRunningHelper : ISprintRunningHelper
    {
        private readonly ISpreadsheetFacade _spreadsheetFacade;

        public SprintRunningHelper(ISpreadsheetFacade spreadsheetFacade)
        {
            _spreadsheetFacade = spreadsheetFacade;
        }

        public List<CellEntry> GetFirstColumn(WorksheetEntry worksheet)
        {
            return _spreadsheetFacade.GetCellsValues(worksheet, 1, uint.MaxValue, 1, 1).ToList();
        }

        public Dictionary<string, uint> GetSectionLinesPosition(WorksheetEntry worksheet, string sectionTitle)
        {
            return GetSectionLinesPosition(GetFirstColumn(worksheet), sectionTitle);
        }

        public Dictionary<string, uint> GetSectionLinesPosition(IEnumerable<CellEntry> firstColumn, string sectionTitle)
        {
            var positions = new Dictionary<string, uint>();
            var cellEntries = firstColumn as IList<CellEntry> ?? firstColumn.ToList();

            CalculateSectionPositions(cellEntries, positions, c => c.Value.Equals(sectionTitle), c => c.Value.Contains("#"));

            return positions;
        }

        public uint GetHeaderColumnPosition(WorksheetEntry worksheet, string sectionTitle, string columnName)
        {
            var sectionLines = GetSectionLinesPosition(worksheet, sectionTitle);
            return GetHeaderColumnPosition(worksheet, sectionLines, columnName);
        }

        public uint GetHeaderColumnPosition(WorksheetEntry worksheet, Dictionary<string, uint> sectionLines, string columnName)
        {
            var rowLine = sectionLines.Min(spp => spp.Value) - 1;
            var headerSectionLine = _spreadsheetFacade.GetCellsValues(worksheet, rowLine, rowLine, 1, uint.MaxValue).ToList();
            return headerSectionLine.Where(h => h.Value.Equals(columnName)).Select(h => h.Column).First();
        }

        public Dictionary<string, DateTime> GetSprintPeriod(WorksheetEntry worksheet)
        {
            var dateFormat = new CultureInfo("pt-BR", false).DateTimeFormat;
            var strStartDate = _spreadsheetFacade.GetCellValue(worksheet, 2, 2);
            var strEndDate = _spreadsheetFacade.GetCellValue(worksheet, 3, 2);

            var startDate = Convert.ToDateTime(strStartDate, dateFormat);
            var endDate = Convert.ToDateTime(strEndDate, dateFormat);

            return new Dictionary<string, DateTime> { { "startDate", startDate }, { "endDate", endDate } };
        }

        public IDictionary<string, double> GetAvailabilityFromNow(WorksheetEntry worksheet, IEnumerable<string> professionals = null)
        {
            var sectionLines = GetSectionLinesPosition(worksheet, "Disponibilidade de horas por profissional e dia");
            var minLine = sectionLines.Select(sl => sl.Value).Min();
            var maxLine = sectionLines.Select(sl => sl.Value).Max();
            var today = DateTime.Now.ToShortDateString();
            var dateLines = _spreadsheetFacade.GetCellsValues(worksheet, minLine - 1, minLine, 1, uint.MaxValue).ToList();
            var colNow = dateLines.First(dl => dl.Value.Equals(today)).Column;
            var result = new Dictionary<string, double>();

            for (var i = minLine; i <= maxLine; i++)
            {
                var i1 = i;
                var professional = sectionLines.Where(sl => sl.Value == i1).Select(sl => sl.Key).First().ToLower();
                var enumerableProfessionals = professionals?.ToArray() ?? new string[0];

                if (!enumerableProfessionals.Any() || enumerableProfessionals.Any(ep => ep.ToLower().Contains(professional) || professional.Contains(ep.ToLower())))
                {
                    var availability = _spreadsheetFacade.GetCellsValues(worksheet, i, i, colNow, uint.MaxValue).Sum(c => double.Parse(c.Value));
                    result.Add(professional, availability);
                }
            }

            return result;
        }

        private void CalculateSectionPositions(IEnumerable<CellEntry> firstColumn, IDictionary<string, uint> positions,
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

                if (inSection && checkIfOutFromSection(cellEntry))
                    break;

                if (inSection && !string.IsNullOrWhiteSpace(cellEntry.Value))
                    positions.Add(cellEntry.Value.Trim(), cellEntry.Row);
            }
        }
    }
}