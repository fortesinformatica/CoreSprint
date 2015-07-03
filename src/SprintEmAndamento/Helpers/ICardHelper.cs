using System.Collections.Generic;
using TrelloNet;

namespace CoreSprint.Helpers
{
    public interface ICardHelper
    {
        string GetUrgency(string priority);
        string GetImportance(string priority);
        string GetCardTitle(Card card);
        Dictionary<string, double> GetWorkedAndRemainder(Card card);
        string GetResponsible(Card card);
        string GetStatus(Card card);
        string GetCardLabels(Card card);
        string GetCardEstimate(Card card);
        string GetCardPriority(Card card);
    }
}