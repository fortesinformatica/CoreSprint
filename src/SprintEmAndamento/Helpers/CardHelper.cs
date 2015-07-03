using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CoreSprint.CoreTrello;
using TrelloNet;

namespace CoreSprint.Helpers
{
    public class CardHelper : ICardHelper
    {
        private readonly ITrelloFacade _trelloFacade;

        private readonly string _strNumberPattern;
        private readonly string _nothingValue;
        private readonly string _delimiter;
        private readonly string _strPriorityPattern;
        private readonly string _strEstimatePattern;

        public CardHelper(ITrelloFacade trelloFacade)
        {
            _trelloFacade = trelloFacade;

            _strNumberPattern = @"[0-9]+[\.,]?[0-9]*";
            _nothingValue = "-";
            _delimiter = ";";
            _strPriorityPattern = @"\[i[0-9]+-u[0-9]+\]";
            _strEstimatePattern = string.Format(@"\{{(\s)*({0})(\s)*hora[\sa-zA-Z]*\}}", _strNumberPattern);
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

        public Dictionary<string, double> GetWorkedAndRemainder(Card card)
        {
            var comments = _trelloFacade.GetActions(card, new[] { ActionType.CommentCard }).OfType<CommentCardAction>().Reverse();
            var numberPattern = new Regex(_strNumberPattern, RegexOptions.IgnoreCase);
            var workedPattern = new Regex(string.Format(@">(\s)*trabalhado(\s)+{0}(\s)+hora[\sa-zA-Z]*", _strNumberPattern), RegexOptions.IgnoreCase);
            var remainderPattern = new Regex(string.Format(@">(\s)*(restam|restante)(\s)+{0}(\s)+hora[\sa-zA-Z]*", _strNumberPattern), RegexOptions.IgnoreCase);
            var worked = 0D;
            double remainder;
            var cardEstimate = GetCardEstimate(card);

            double.TryParse(numberPattern.Match(cardEstimate).Value, out remainder);

            //TODO: refatorar
            foreach (var comment in comments)
            {
                var matchesWorked = workedPattern.Matches(comment.Data.Text);
                var matchesRemainder = remainderPattern.Matches(comment.Data.Text);

                foreach (Match match in matchesWorked)
                {
                    var matchNumber = numberPattern.Match(match.Value);
                    var workedInComment = double.Parse(matchNumber.Value);
                    worked += matchNumber.Success ? workedInComment : 0D;
                    remainder -= matchesRemainder.Count > 0 ? 0D : workedInComment;
                }

                foreach (Match match in matchesRemainder)
                {
                    var matchNumber = numberPattern.Match(match.Value);
                    remainder = matchNumber.Success ? double.Parse(matchNumber.Value) : 0D;
                }
            }

            remainder = remainder > 0 ? remainder : 0;

            return new Dictionary<string, double> { { "worked", worked }, { "remainder", remainder } };
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
            var numberPattern = new Regex(_strNumberPattern, RegexOptions.IgnoreCase);

            if (matchPattern.Success)
            {
                var numberMatch = numberPattern.Match(matchPattern.Value);
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
    }
}
