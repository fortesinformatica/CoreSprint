using System.Collections.Generic;
using Google.GData.Spreadsheets;

namespace CoreSprint.Helpers
{
    public interface IWorksheetHelper
    {
        WorksheetEntry RedoWorksheet(string spreadsheetId, string worksheetName, List<string> headerNames);
    }
}