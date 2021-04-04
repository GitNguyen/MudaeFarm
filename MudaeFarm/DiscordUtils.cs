using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Disqord;

namespace MudaeFarm
{
    public static class DiscordUtils
    {
        static readonly Regex _userRegex = new Regex(@"<@(?<id>\d+)>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        static readonly Regex _channelRegex = new Regex(@"<#(?<id>\d+)>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

        public static IEnumerable<ulong> GetUserIds(this IMessage message) => message.Match(_userRegex);
        public static IEnumerable<ulong> GetChannelIds(this IMessage message) => message.Match(_channelRegex);

        static IEnumerable<ulong> Match(this IMessage message, Regex regex) =>
            regex.Matches(message.Content)
                 .Select(m =>
                  {
                      ulong.TryParse(m.Groups["id"].Value, out var x);
                      return x;
                  })
                 .Where(id => id != 0);
    }
}