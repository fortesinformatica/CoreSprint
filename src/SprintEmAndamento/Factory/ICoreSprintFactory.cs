using CoreSprint.Helpers;
using CoreSprint.Spreadsheet;
using CoreSprint.Trello;

namespace CoreSprint.Factory
{
    public interface ICoreSprintFactory
    {
        ITrelloConnection GetTrelloConnection();
        ISpreadsheetConnection GetSpreadsheetConnection();
        ITrelloFacade GetTrelloFacade();
        ISpreadsheetFacade GetSpreadsheetFacade();
        ICardHelper GetCardHelper();
        IWorksheetHelper GetWorksheetHelper();
        ISprintRunningHelper GetSprintRunningHelper();
    }
}