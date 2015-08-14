using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using CoreSprint.Extensions;
using CoreSprint.Models;
using CoreSprint.Trello;
using TrelloNet;

namespace CoreSprint.Helpers
{
    public class CardHelper : ICardHelper
    {
        private readonly ITrelloFacade _trelloFacade;
        private readonly ICommentHelper _commentHelper;

        private readonly string _strNumberPattern;
        private readonly string _nothingValue;
        private readonly string _delimiter;
        private readonly string _strPriorityPattern;
        private readonly string _strEstimatePattern;
        private readonly Regex _pendingPattern;
        private readonly Regex _workedPattern;
        private readonly Regex _numberPattern;
        private readonly CultureInfo _cultureInfoEnUs;

        public CardHelper(ITrelloFacade trelloFacade, ICommentHelper commentHelper)
        {
            _trelloFacade = trelloFacade;
            _commentHelper = commentHelper;

            _strNumberPattern = @"[0-9]+[\.,]?[0-9]*";
            _nothingValue = "-";
            _delimiter = ";";

            _strPriorityPattern = @"\[i[0-9]+-u[0-9]+\]";
            _strEstimatePattern = $@"\{{(\s)*({_strNumberPattern})(\s)*hora[\sa-zA-Z]*\}}";

            _numberPattern = new Regex(_strNumberPattern, RegexOptions.IgnoreCase);
            _pendingPattern =
                new Regex($@">(\s)*(restam|restante)(\s)+{_strNumberPattern}",
                    RegexOptions.IgnoreCase);
            _workedPattern = new Regex($@">(\s)*trabalhado(\s)+{_strNumberPattern}",
                RegexOptions.IgnoreCase);

            _cultureInfoEnUs = new CultureInfo("en-US");
        }

        public string GetUrgency(string priority)
        {
            var pattern = new Regex(@"u[0-9]+", RegexOptions.IgnoreCase);
            var patternNumber = new Regex(_strNumberPattern);
            var matchUrgency = pattern.Match(priority);
            return matchUrgency.Success ? patternNumber.Match(matchUrgency.Value).Value : "0";
        }

        public string GetImportance(string priority)
        {
            var pattern = new Regex(@"i[0-9]+", RegexOptions.IgnoreCase);
            var patternNumber = new Regex(_strNumberPattern);
            var matchUrgency = pattern.Match(priority);
            return matchUrgency.Success ? patternNumber.Match(matchUrgency.Value).Value : "0";
        }
        public string GetCardTitle(Card card)
        {
            var replacedPriority = Regex.Replace(card.Name, _strPriorityPattern, "", RegexOptions.IgnoreCase);
            return Regex.Replace(replacedPriority, _strEstimatePattern, "", RegexOptions.IgnoreCase).Trim();
        }

        public IEnumerable<CommentCardAction> GetCardComments(Card card)
        {
            var comments = _trelloFacade.GetActions(card, new[] { ActionType.CommentCard }).OfType<CommentCardAction>();
            var sortComparer = new CommentSortComparer(_commentHelper);
            var commentCardActions = comments as IList<CommentCardAction> ?? comments.ToList();
            var cardActions = commentCardActions as List<CommentCardAction>;

            cardActions?.Sort(sortComparer);

            return cardActions;
        }

        public string GetResponsible(Card card)
        {
            return card.IdMembers.Any()
                ? _trelloFacade.GetMembers(card).Select(m => m.FullName).Aggregate((i, j) => i + _delimiter + j)
                : _nothingValue;
        }
        public string GetStatus(Card card)
        {
            return _trelloFacade.GetList(card).Name.Split('\n').FirstOrDefault();
        }
        public string GetCardLabels(Card card)
        {
            return card.Labels.Any()
                ? card.Labels.Select(l => l.Name).Aggregate((i, j) => i + _delimiter + j)
                : _nothingValue;
        }

        public string GetCardEstimate(Card card)
        {
            var estimatePattern = new Regex(_strEstimatePattern, RegexOptions.IgnoreCase);
            var matchPattern = estimatePattern.Match(card.Name);

            if (matchPattern.Success)
            {
                var numberMatch = _numberPattern.Match(matchPattern.Value);
                return numberMatch.Success ? numberMatch.Value.Replace(".", ",") : "0";
            }
            return "0";
        }

        public string GetCardPriority(Card card)
        {
            var pattern = new Regex(_strPriorityPattern, RegexOptions.IgnoreCase);
            var match = pattern.Match(card.Name);
            return match.Success ? match.Value : _nothingValue;
        }

        public Dictionary<string, double> GetWorkedAndPending(Card card)
        {
            var comments = GetCardComments(card);
            return GetWorkedAndPending(GetCardEstimate(card), comments);
        }

        public Dictionary<string, double> GetWorkedAndPending(Card card, DateTime until)
        {
            var comments = GetCardComments(card);
            return GetWorkedAndPending(GetCardEstimate(card), comments, until);
        }

        public Dictionary<string, double> GetWorkedAndPending(string cardEstimate, IEnumerable<CommentCardAction> comments)
        {
            return GetWorkedAndPending(cardEstimate, comments, null);
        }

        public Dictionary<string, double> GetWorkedAndPending(string cardEstimate, IEnumerable<CommentCardAction> comments, DateTime until)
        {
            return GetWorkedAndPending(cardEstimate, comments, c => _commentHelper.GetDateInComment(c) <= until);
        }

        public Dictionary<string, double> GetWorkedAndPending(string cardEstimate, IEnumerable<CommentCardAction> comments, DateTime startDate, DateTime endDate)
        {
            return GetWorkedAndPending(cardEstimate, comments, c =>
            {
                var dateInComment = _commentHelper.GetDateInComment(c);
                return dateInComment >= startDate && dateInComment <= endDate;
            });
        }

        public Dictionary<string, double> GetWorkedAndPending(string cardEstimate, List<CommentCardAction> comments, string professional,
            DateTime startDate, DateTime endDate)
        {
            return GetWorkedAndPending(cardEstimate, comments,
                c =>
                {
                    var dateInComment = _commentHelper.GetDateInComment(c);
                    return dateInComment >= startDate &&
                           dateInComment <= endDate &&
                           professional.Equals(c.MemberCreator.FullName);
                });
        }

        public IEnumerable<CardWorkDto> GetCardWorkExtract(Card card, DateTime startDate, DateTime endDate, string professional = null)
        {
            var extract = new List<CardWorkDto>();
            var comments = GetCardComments(card).AsParallel().AsOrdered();
            var workedControl = new Dictionary<string, DateTime>();

            professional = professional?.ToLower();

            comments = professional == null
                ? comments
                : comments.Where(c =>
                {
                    var professionalInComment = c.MemberCreator.FullName.ToLower();
                    return professionalInComment.Contains(professional) || professional.Contains(professionalInComment);
                });

            foreach (var comment in comments)
            {
                var dateInComment = _commentHelper.GetDateInComment(comment);
                var matchWork = _workedPattern.Match(comment.Data.Text).Success ||
                                _commentHelper.HasStartPattern(comment.Data.Text) ||
                                _commentHelper.HasStopPattern(comment.Data.Text);

                if (matchWork && dateInComment >= startDate && dateInComment <= endDate)
                {
                    var worked = CalculateRunningWorked(comment, workedControl) +
                                 CalculateWorked(comment, _cultureInfoEnUs);

                    extract.Add(new CardWorkDto
                    {
                        Professional = comment.MemberCreator.FullName,
                        CardName = GetCardTitle(card),
                        CardLink = card.ShortUrl,
                        CommentAt = comment.Date.ConvertUtcToFortalezaTimeZone(),
                        WorkAt = dateInComment,
                        Worked = worked,
                        Comment = comment.Data.Text
                    });
                }
            }

            return extract;
        }

        public IEnumerable<CardWorkDto> GetCardsWorkExtract(IEnumerable<Card> cards, DateTime startDate, DateTime endDate, string professional = null)
        {
            var i = 0;
            IEnumerable<CardWorkDto> allWork = new List<CardWorkDto>();
            var enumerableCards = cards as IList<Card> ?? cards.ToList();
            var count = enumerableCards.Count();

            allWork =
                enumerableCards.AsParallel().AsOrdered().Aggregate(allWork,
                    (current, card) =>
                    {
                        Console.WriteLine("Analisando cartão ({0}/{1}) {2}", ++i, count, card.Name);
                        return
                            current.Concat(
                                ExecutionHelper.ExecuteAndRetryOnFail(
                                    () => GetCardWorkExtract(card, startDate, endDate, professional)));
                    })
                    .OrderBy(w => w.Professional)
                    .ThenBy(w => w.CardLink)
                    .ThenBy(w => w.WorkAt)
                    .ToList();
            return allWork;
        }

        private Dictionary<string, double> GetWorkedAndPending(string cardEstimate, IEnumerable<CommentCardAction> comments, Func<CommentCardAction, bool> validateComment)
        {
            var strPending = _numberPattern.Match(cardEstimate.Replace(",", ".")).Value;
            var pending = double.Parse(string.IsNullOrWhiteSpace(strPending) ? "0" : strPending, _cultureInfoEnUs);
            var worked = 0D;
            var workedControl = new Dictionary<string, DateTime>();

            comments = comments.AsParallel().AsOrdered();

            foreach (var comment in comments.Where(comment => validateComment == null || validateComment(comment)))
            {
                var pendingInComment = CalculatePending(comment, _cultureInfoEnUs);
                var workedInComment = CalculateRunningWorked(comment, workedControl) +
                                      CalculateWorked(comment, _cultureInfoEnUs);

                worked += workedInComment;
                pending = pendingInComment ?? pending - workedInComment;
            }

            pending = pending > 0 ? pending : 0;

            return new Dictionary<string, double> { { "worked", worked }, { "pending", pending } };
        }

        private double CalculateWorked(CommentCardAction comment, CultureInfo cultureInfo)
        {
            var worked = 0D;
            var matchesWorked = _workedPattern.Matches(comment.Data.Text);

            foreach (Match match in matchesWorked)
            {
                var matchNumber = _numberPattern.Match(match.Value);
                var workedInComment = double.Parse(matchNumber.Value, cultureInfo);
                worked += matchNumber.Success ? workedInComment : 0D;
            }
            return worked;
        }

        private double? CalculatePending(CommentCardAction comment, CultureInfo cultureInfo)
        {
            var matchesPending = _pendingPattern.Matches(comment.Data.Text);
            var pending = (double?)null;

            foreach (var matchNumber in from Match match in matchesPending select _numberPattern.Match(match.Value))
            {
                pending = matchNumber.Success ? double.Parse(matchNumber.Value, cultureInfo) : pending;
            }
            return pending;
        }

        private double CalculateRunningWorked(CommentCardAction comment, IDictionary<string, DateTime> workedControl)
        {
            double runningWorked = 0;

            if (_commentHelper.HasStartPattern(comment.Data.Text))
                workedControl[comment.IdMemberCreator] = _commentHelper.GetDateInComment(comment);

            if (workedControl.ContainsKey(comment.IdMemberCreator))
            {
                if (_commentHelper.HasStopPattern(comment.Data.Text))
                {
                    var dateTimeWorkStarted = workedControl[comment.IdMemberCreator];
                    var workRunning = _commentHelper.GetDateInComment(comment) - dateTimeWorkStarted;
                    runningWorked += workRunning.TotalHours > 0 ? workRunning.TotalHours : 0;

                    workedControl.Remove(comment.IdMemberCreator);
                }
            }
            return runningWorked;
        }
    }

    class CommentSortComparer : IComparer<CommentCardAction>
    {
        private readonly ICommentHelper _commentHelper;

        public CommentSortComparer(ICommentHelper commentHelper)
        {
            _commentHelper = commentHelper;
        }

        public int Compare(CommentCardAction x, CommentCardAction y)
        {
            var dateInCommentX = _commentHelper.GetDateInComment(x);
            var dateInCommentY = _commentHelper.GetDateInComment(y);

            return dateInCommentX.Equals(dateInCommentY) ? 0 : dateInCommentX > dateInCommentY ? 1 : -1;
        }
    }
}
