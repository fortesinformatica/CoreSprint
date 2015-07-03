﻿using System;
using System.Collections.Generic;
using System.Linq;
using Google.GData.Spreadsheets;

namespace CoreSprint.CoreSpreadsheet
{
    public class SpreadsheetSprint
    {
        private readonly SpreadsheetConnection _connection;

        public SpreadsheetSprint(SpreadsheetConnection connection)
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
            //TODO: receber parâmetros com a lista de células
            var cellQuery = new CellQuery(worksheet.CellFeedLink);
            var cellFeed = _connection.SpreadsheetService.Query(cellQuery);

            for (var i = 0; i < cellHeaders.Count; i++)
            {
                var cellEntry = new CellEntry(1, (uint)(i + 1), cellHeaders[i]);
                cellFeed.Insert(cellEntry);
            }
        }

        public void InsertInWorksheet(WorksheetEntry worksheet, ListEntry row)
        {
            var listFeedLink = worksheet.Links.FindService(GDataSpreadsheetsNameTable.ListRel, null);
            var listQuery = new ListQuery(listFeedLink.HRef.ToString());
            var listFeed = _connection.SpreadsheetService.Query(listQuery);

            _connection.SpreadsheetService.Insert(listFeed, row);
        }
    }
}