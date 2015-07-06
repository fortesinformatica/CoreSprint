using System;
using System.Collections.Generic;
using TrelloNet;

namespace CoreSprint.Helpers
{
    public interface ICardHelper
    {
        string GetUrgency(string priority);
        string GetImportance(string priority);
        string GetCardTitle(Card card);
        string GetResponsible(Card card);
        string GetStatus(Card card);
        string GetCardLabels(Card card);
        string GetCardEstimate(Card card);
        string GetCardPriority(Card card);
        IEnumerable<CommentCardAction> GetCardComments(Card card);
        Dictionary<string, double> GetWorkedAndRemainder(Card card);
        Dictionary<string, double> GetWorkedAndRemainder(Card card, DateTime until);
        Dictionary<string, double> GetWorkedAndRemainder(string cardEstimate, IEnumerable<CommentCardAction> comments);
        Dictionary<string, double> GetWorkedAndRemainder(string cardEstimate, IEnumerable<CommentCardAction> comments, DateTime until);
        Dictionary<string, double> GetWorkedAndRemainder(string cardEstimate, IEnumerable<CommentCardAction> comments, DateTime startDate, DateTime endDate);
        Dictionary<string, double> GetWorkedAndRemainder(string cardEstimate, List<CommentCardAction> comments, string professional, DateTime startDate, DateTime endDate);
    }
}