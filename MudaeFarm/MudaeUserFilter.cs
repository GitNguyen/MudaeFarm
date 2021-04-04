using System;
using System.Text.RegularExpressions;
using Disqord;

namespace MudaeFarm
{
    public interface IMudaeUserFilter
    {
        bool IsMudae(IUser user);
    }

    public class MudaeUserFilter : IMudaeUserFilter
    {
        static readonly ulong[] _ids =
        {
            432610292342587392, // main Mudae bot
            479206206725160960  // the first maid "Mudamaid" which doesn't match _nameRegex
        };

        static readonly Regex _nameRegex = new Regex(@"^Mudae?(maid|butler)\s*\d+$", RegexOptions.Singleline | RegexOptions.Compiled);

        public bool IsMudae(IUser user) => user.IsBot && (Array.IndexOf(_ids, user.Id) != -1 || _nameRegex.IsMatch(user.Name));
    }
}