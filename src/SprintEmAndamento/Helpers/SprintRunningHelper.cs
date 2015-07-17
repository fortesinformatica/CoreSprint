using System;
using System.Collections.Generic;
using System.Linq;
using CoreSprint.Spreadsheet;
using Google.GData.Spreadsheets;

namespace CoreSprint.Helpers
{
    public interface ISprintRunningHelper
    {
        List<CellEntry> GetFirstColumn(WorksheetEntry worksheet);
        Dictionary<string, uint> GetSectionLinesPosition(WorksheetEntry worksheet, string sectionTitle);
        Dictionary<string, uint> GetSectionLinesPosition(IEnumerable<CellEntry> firstColumn, string sectionTitle);
        uint GetHeaderColumnPosition(WorksheetEntry worksheet, string sectionTitle, string columnName);
        uint GetHeaderColumnPosition(WorksheetEntry worksheet, Dictionary<string, uint> sectionLines, string columnName);
    }

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
    }
}