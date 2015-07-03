using TrelloNet;

namespace CoreSprint.CoreTrello
{
    public interface ITrelloConnection
    {
        ITrello Trello { get; }
        bool IsAuthenticated();
    }
}