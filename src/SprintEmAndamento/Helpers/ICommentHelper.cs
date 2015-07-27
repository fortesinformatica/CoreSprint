using System;
using TrelloNet;

namespace CoreSprint.Helpers
{
    public interface ICommentHelper
    {
        bool HasStartPattern(string commentText);
        bool HasStopPattern(string commentText);
        DateTime GetDateInComment(CommentCardAction comment);
    }
}