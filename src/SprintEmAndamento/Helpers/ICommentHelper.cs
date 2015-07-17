using System;
using TrelloNet;

namespace CoreSprint.Helpers
{
    public interface ICommentHelper
    {
        DateTime GetDateInComment(CommentCardAction comment);
    }
}