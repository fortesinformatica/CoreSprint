using Google.GData.Spreadsheets;

namespace CoreSprint.Spreadsheet
{
    public interface ISpreadsheetConnection
    {
        SpreadsheetsService SpreadsheetService { get; }
    }
}