using System;
using System.Collections.Generic;
using CoreSprint.Spreadsheet;
using Google.GData.Spreadsheets;

namespace CoreSprint.Helpers
{
    public class WorksheetHelper : IWorksheetHelper
    {
        private readonly ISpreadsheetFacade _spreadsheetFacade;

        public WorksheetHelper(ISpreadsheetFacade spreadsheetFacade)
        {
            _spreadsheetFacade = spreadsheetFacade;
        }

        public WorksheetEntry RedoWorksheet(SpreadsheetEntry spreadsheet, string worksheetName, List<string> headerNames)
        {
            var createdTempWorksheet = false;

            if (spreadsheet.Worksheets.Entries.Count <= 1)
            {
                _spreadsheetFacade.CreateWorksheet(spreadsheet, "Temp", 1, 1);
                createdTempWorksheet = true;
            }

            _spreadsheetFacade.DeleteWorksheet(spreadsheet, worksheetName); //TODO: fazer backup
            _spreadsheetFacade.CreateWorksheet(spreadsheet, worksheetName, 1, (uint)headerNames.Count);

            if (createdTempWorksheet)
                _spreadsheetFacade.DeleteWorksheet(spreadsheet, "Temp");

            var worksheet = _spreadsheetFacade.GetWorksheet(spreadsheet, worksheetName);
            _spreadsheetFacade.CreateHeader(worksheet, headerNames);
            return worksheet;
        }

        public WorksheetEntry RedoWorksheet(string spreadsheetId, string worksheetName, List<string> headerNames)
        {
            Console.WriteLine("Recriando {0}...", worksheetName);

            var spreadsheet = _spreadsheetFacade.GetSpreadsheet(spreadsheetId);
            return RedoWorksheet(spreadsheet, worksheetName, headerNames);
        }
    }
}
