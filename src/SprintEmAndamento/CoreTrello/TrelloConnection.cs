﻿using TrelloNet;

namespace CoreSprint.CoreTrello
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

            Trello = Trello ?? new Trello(appKey);
            Trello.Authorize(userToken);
        }
    }
}