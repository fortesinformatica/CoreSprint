using System.Collections.Generic;
using TrelloNet;

namespace CoreSprint.CoreTrello
{
    public interface ITrelloFacade
    {
        IEnumerable<Card> GetCards(string boardId);
        IEnumerable<Card> GetCards(Board board);
        Board GetBoard(string boardId);
        IEnumerable<Action> GetActions(Card card, ActionType[] actionTypes);
        List GetList(Card card);
        IEnumerable<Member> GetMembers(Card card);
    }
}