using CoreSprint.CoreSpreadsheet;
using CoreSprint.CoreTrello;
using CoreSprint.Helpers;

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
    }
}