using System.Collections.Generic;
using TrelloNet;

namespace CoreSprint.CoreTrello
{
    public class TrelloFacade : ITrelloFacade
    {
        private readonly ITrelloConnection _connection;

        public TrelloFacade(ITrelloConnection connection)
        {
            _connection = connection;
        }

        public IEnumerable<Card> GetCards(string boardId)
        {
            var board = GetBoard(boardId);
            return GetCards(board);
        }

        public IEnumerable<Card> GetCards(Board board)
        {
            if (board != null && _connection.IsAuthenticated())
                return _connection.Trello.Cards.ForBoard(board);
            return new List<Card>();
        }

        public Board GetBoard(string boardId)
        {
            if (_connection.IsAuthenticated())
                return _connection.Trello.Boards.WithId(boardId);
            return null;
        }

        public IEnumerable<Action> GetActions(Card card, ActionType[] actionTypes)
        {
            return _connection.Trello.Actions.ForCard(card, new[] { ActionType.CommentCard });
        }

        public List GetList(Card card)
        {
            return _connection.Trello.Lists.ForCard(card);
        }

        public IEnumerable<Member> GetMembers(Card card)
        {
            return _connection.Trello.Members.ForCard(card);
        }
    }
}