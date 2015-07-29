using CoreSprint.Helpers;
using CoreSprint.Integration;
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
        ICommentHelper GetCommentHelper();
        IWorksheetHelper GetWorksheetHelper();
        ISprintRunningHelper GetSprintRunningHelper();
        ITelegramHelper GetTelegramHelper();
        ICommand GetRunningSprintUpdater(string trelloBoardId, string spreadsheetId);
    }
}