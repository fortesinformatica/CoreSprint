using System.Collections.Generic;
using Google.GData.Spreadsheets;

namespace CoreSprint.CoreSpreadsheet
{
    public interface ISpreadsheetFacade
    {
        SpreadsheetEntry GetSpreadsheet(string spreadsheetId);
        void CreateWorksheet(SpreadsheetEntry spreadsheet, string worksheeTitle, uint rows = 100, uint cols = 10);
        void CreateWorksheet(string spreadsheetId, string worksheeTitle, uint rows = 100, uint cols = 10);
        void DeleteWorksheet(string spreadsheetId, string worksheetName);
        void DeleteWorksheet(SpreadsheetEntry spreadsheet, string worksheetName);
        WorksheetEntry GetWorksheet(string spreadsheetId, string worksheetName);
        WorksheetEntry GetWorksheet(SpreadsheetEntry spreadsheet, string worksheetName);
        void CreateHeader(WorksheetEntry worksheet, IList<string> cellHeaders);
        void InsertInWorksheet(WorksheetEntry worksheet, ListEntry row);
        string GetCellValue(WorksheetEntry worksheet, uint row, uint col);
        IEnumerable<CellEntry> GetCellValue(WorksheetEntry worksheet, uint minrow, uint maxrow, uint mincol, uint maxcol);
        void SaveToCell(WorksheetEntry worksheet, uint row, uint col, string value);
    }
}