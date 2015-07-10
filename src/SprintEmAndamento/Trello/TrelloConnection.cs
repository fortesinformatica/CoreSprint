using TrelloNet;

namespace CoreSprint.Trello
{
    public class TrelloConnection : ITrelloConnection
    {
        public ITrello Trello { get; private set; }

        public TrelloConnection(string appKey, string userToken)
        {
            Authenticate(appKey, userToken);
        }

        public bool IsAuthenticated()
        {
            //TODO: adicionar mais validações para garantir que a autenticação foi feita
            return Trello != null;
        }

        private void Authenticate(string appKey, string userToken)
        {
            //TODO: tratar caso de appKey ou userToken inválido

            Trello = Trello ?? new TrelloNet.Trello(appKey);
            Trello.Authorize(userToken);
        }
    }
}