using TrelloNet;

namespace CoreSprint.Trello
{
    public interface ITrelloConnection
    {
        ITrello Trello { get; }
        bool IsAuthenticated();
    }
}