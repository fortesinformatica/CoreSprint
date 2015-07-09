using System;
using System.Globalization;
using System.Text.RegularExpressions;
using TrelloNet;

namespace CoreSprint.Helpers
{
    public interface ICommentHelper
    {
        DateTime GetDateInComment(CommentCardAction comment);
    }

    public class CommentHelper : ICommentHelper
    {
        public DateTime GetDateInComment(CommentCardAction comment)
        {
            var strDatePattern = @"[0-9][0-9]/[0-9][0-9]/[0-9][0-9][0-9][0-9]";
            var strPattern = string.Format(@">(.)*{0}", strDatePattern);
            var pattern = new Regex(strPattern, RegexOptions.IgnoreCase);
            var match = pattern.Match(comment.Data.Text);

            if (match.Success)
            {
                var datePattern = new Regex(strDatePattern, RegexOptions.IgnoreCase);
                var dateMatch = datePattern.Match(match.Value);
                var dateFormat = new CultureInfo("pt-BR", false).DateTimeFormat;
                
                return Convert.ToDateTime(dateMatch.Value, dateFormat);
            }

            return comment.Date;
        }
    }
}
