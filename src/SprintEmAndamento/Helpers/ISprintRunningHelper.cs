using System;
using System.Collections.Generic;
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
        Dictionary<string, DateTime> GetSprintPeriod(WorksheetEntry worksheet);
        IDictionary<string, double> GetAvailabilityFromNow(WorksheetEntry worksheet,IEnumerable<string> professionals = null);
    }
}