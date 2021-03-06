﻿using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using CoreSprint.Extensions;
using TrelloNet;

namespace CoreSprint.Helpers
{
    public class CommentHelper : ICommentHelper
    {
        public bool HasStartPattern(string commentText)
        {
            var startWorkPattern = new Regex(@">(\s)*inicia");
            return startWorkPattern.Match(commentText).Success;
        }

        public bool HasStopPattern(string commentText)
        {
            var stopWorkPattern = new Regex(@">(\s)*(pausa|para)");
            return stopWorkPattern.Match(commentText).Success;
        }

        public DateTime GetDateInComment(CommentCardAction comment)
        {
            //TODO: não deve ter trabalhado, pausa ou início simultâneos no mesmo comentário
            const string strDatePattern = @"[0-9][0-9]/[0-9][0-9]/[0-9][0-9][0-9][0-9]";

            var strPatternWorked = $@">(.)*{strDatePattern}";
            var patternWorked = new Regex(strPatternWorked, RegexOptions.IgnoreCase);
            var matchWorked = patternWorked.Match(comment.Data.Text);

            if (matchWorked.Success)
            {
                var dateFormat = new CultureInfo("pt-BR", false).DateTimeFormat;
                var datePattern = new Regex(strDatePattern, RegexOptions.IgnoreCase);
                var dateMatch = datePattern.Match(matchWorked.Value);

                var dateInComment = Convert.ToDateTime(dateMatch.Value, dateFormat);
                return dateInComment;
            }

            return GetDateTimeChangedWhenTimeInformedInWork(comment) ?? comment.Date.ConvertUtcToFortalezaTimeZone();
        }

        private static DateTime? GetDateTimeChangedWhenTimeInformedInWork(CommentCardAction comment)
        {
            DateTime? dateChanged = null;
            const string strWorkPattern = @">(\s)*(pausa|para|inicia)";
            const string strHourPattern = @"[0-2]?[0-9]:[0-5]?[0-9]";

            var strStopedWork = $@"{strWorkPattern}(.)*{strHourPattern}";
            var stopWorkPattern = new Regex(strStopedWork);
            var matchStopWork = stopWorkPattern.Match(comment.Data.Text);

            if (matchStopWork.Success)
            {
                var dateFormat = new CultureInfo("pt-BR", false).DateTimeFormat;
                var hourPattern = new Regex(strHourPattern);
                var matchHourPattern = hourPattern.Match(matchStopWork.Value);
                var hourText = matchHourPattern.Value.Split(':').Aggregate((text, next) => text.PadLeft(2, '0') + ":" + next.PadLeft(2, '0'));
                var dateTime = comment.Date.ConvertUtcToFortalezaTimeZone();
                var strDateStopWork = $"{dateTime.Year}/{dateTime.Month}/{dateTime.Day} {hourText}:00";

                dateChanged = Convert.ToDateTime(strDateStopWork, dateFormat);
            }
            return dateChanged;
        }
    }
}
