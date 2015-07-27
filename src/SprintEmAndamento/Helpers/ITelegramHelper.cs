using System.Collections.Generic;

namespace CoreSprint.Helpers
{
    public interface ITelegramHelper
    {
        IEnumerable<string> GetQueryResponsible(string message, string commandName = "");
    }
}