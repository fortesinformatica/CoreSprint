using System.Collections.Generic;
using System.Linq;

namespace CoreSprint.Helpers
{
    public class TelegramHelper : ITelegramHelper
    {
        public IEnumerable<string> GetQueryResponsible(string message, string commandName = "")
        {
            var commandText = message.Replace($"@{CoreSprintApp.TelegramBotName}", "").Split(' ').First(s => !string.IsNullOrWhiteSpace(s));
            commandText = commandText.Replace($"/{commandName}", ""); //TODO: não há garantias que este será o nome do comando
            return commandText.Split('_').Where(s => !string.IsNullOrWhiteSpace(s));
        }
    }
}