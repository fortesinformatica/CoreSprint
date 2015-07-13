using System;
using System.Collections.Generic;
using System.Linq;
using Google.GData.Spreadsheets;

namespace CoreSprint.Spreadsheet
{
    public class SpreadsheetFacade : ISpreadsheetFacade
    {
        private readonly ISpreadsheetConnection _connection;

        public SpreadsheetFacade(ISpreadsheetConnection connection)
        {
            _connection = connection;
        }

        public SpreadsheetEntry GetSpreadsheet(string spreadsheetId)
        {
            //TODO: tratar se está autenticado
            var spreadsheetQuery = new SpreadsheetQuery
            {
                Uri = new Uri(string.Format("https://spreadsheets.google.com/feeds/spreadsheets/private/full/{0}", spreadsheetId))
            };
            var spreadsheets = _connection.SpreadsheetService.Query(spreadsheetQuery);
            return spreadsheets.Entries.FirstOrDefault() as SpreadsheetEntry;
        }

        public void CreateWorksheet(SpreadsheetEntry spreadsheet, string worksheeTitle, uint rows = 100, uint cols = 10)
        {
            //TODO: tratar se está autenticado
            var worksheet = new WorksheetEntry(rows, cols, worksheeTitle);
            _connection.SpreadsheetService.Insert(spreadsheet.Worksheets, worksheet);
        }

        public void CreateWorksheet(string spreadsheetId, string worksheeTitle, uint rows = 100, uint cols = 10)
        {
            //TODO: tratar se está autenticado
            var spreadsheet = GetSpreadsheet(spreadsheetId);
            CreateWorksheet(spreadsheet, worksheeTitle, rows, cols);
        }

        public void DeleteWorksheet(string spreadsheetId, string worksheetName)
        {
            //TODO: tratar se está autenticado
            var spreadsheet = GetSpreadsheet(spreadsheetId);
            DeleteWorksheet(spreadsheet, worksheetName);
        }

        public void DeleteWorksheet(SpreadsheetEntry spreadsheet, string worksheetName)
        {
            //TODO: tratar se está autenticado
            var worksheet = spreadsheet.Worksheets.Entries.FirstOrDefault(e => e.Title.Text == worksheetName);
            if (worksheet != null)
                worksheet.Delete();
        }

        public WorksheetEntry GetWorksheet(string spreadsheetId, string worksheetName)
        {
            var spreadsheet = GetSpreadsheet(spreadsheetId);
            return GetWorksheet(spreadsheet, worksheetName);
        }

        public WorksheetEntry GetWorksheet(SpreadsheetEntry spreadsheet, string worksheetName)
        {
            return (WorksheetEntry)spreadsheet.Worksheets.Entries.FirstOrDefault(e => e.Title.Text == worksheetName);
        }

        public void CreateHeader(WorksheetEntry worksheet, IList<string> cellHeaders)
        {
            var cellFeed = GetCellFeed(worksheet);

            for (var i = 0; i < cellHeaders.Count; i++)
            {
                var cellEntry = new CellEntry(1, (uint)(i + 1), cellHeaders[i]);
                cellFeed.Insert(cellEntry);
            }
        }

        public void InsertInWorksheet(WorksheetEntry worksheet, ListEntry row)
        {
            var listFeed = GetListFeed(worksheet);
            _connection.SpreadsheetService.Insert(listFeed, row);
        }

        public string GetCellValue(WorksheetEntry worksheet, uint row, uint col)
        {
            var cellEntry = GetCellsValues(worksheet, row, row, col, col).FirstOrDefault();
            return cellEntry != null ? cellEntry.InputValue : "";
        }

        public IEnumerable<CellEntry> GetCellsValues(WorksheetEntry worksheet, uint minrow, uint maxrow, uint mincol, uint maxcol)
        {
            var cellQuery = new CellQuery(worksheet.CellFeedLink)
            {
                MinimumColumn = mincol,
                MaximumColumn = maxcol,
                MinimumRow = minrow,
                MaximumRow = maxrow
            };

            var cellFeed = GetCellFeed(cellQuery);
            return cellFeed.Entries.OfType<CellEntry>();
        }

        public void SaveToCell(WorksheetEntry worksheet, uint row, uint col, string value)
        {
            var cellFeed = GetCellFeed(worksheet);
            var cellEntry = new CellEntry(row, col, value);
            cellFeed.Insert(cellEntry);
        }

        private CellFeed GetCellFeed(CellQuery cellQuery)
        {
            return _connection.SpreadsheetService.Query(cellQuery);
        }

        private ListFeed GetListFeed(WorksheetEntry worksheet)
        {
            var listFeedLink = worksheet.Links.FindService(GDataSpreadsheetsNameTable.ListRel, null);
            var listQuery = new ListQuery(listFeedLink.HRef.ToString());
            var listFeed = _connection.SpreadsheetService.Query(listQuery);
            return listFeed;
        }

        private CellFeed GetCellFeed(WorksheetEntry worksheet)
        {
            var cellQuery = new CellQuery(worksheet.CellFeedLink);
            var cellFeed = _connection.SpreadsheetService.Query(cellQuery);
            return cellFeed;
        }
    }
}