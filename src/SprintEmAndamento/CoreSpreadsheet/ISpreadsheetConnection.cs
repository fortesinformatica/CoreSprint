using Google.GData.Spreadsheets;

namespace CoreSprint.CoreSpreadsheet
{
    public interface ISpreadsheetConnection
    {
        SpreadsheetsService SpreadsheetService { get; }
    }
}