using System;
using System.Text.RegularExpressions;

namespace MudaeFarm
{
    public interface IMudaeOutputParser
    {
        bool TryParseTime(string s, out TimeSpan time);
        bool TryParseRollRemaining(string s, out int count);
        bool TryParseRollLimited(string s, out TimeSpan resetTime);
        bool TryParseClaimSucceeded(string s, out string claimer, out string claimed);
        bool TryParseClaimFailed(string s, out TimeSpan resetTime);
        bool TryParseKakeraSucceeded(string s, out string claimer, out int claimed);
        bool TryParseKakeraFailed(string s, out TimeSpan resetTime);
    }

    public class EnglishMudaeOutputParser : IMudaeOutputParser
    {
        const RegexOptions _regexOptions = RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase;

        static readonly Regex _timeRegex = new Regex(@"((?<hour>\d+)h\s*)?(?<minute>\d+)(\**)?\s*min", _regexOptions);

        public bool TryParseTime(string s, out TimeSpan time)
        {
            var match = _timeRegex.Match(s);

            int.TryParse(match.Groups["minute"].Value, out var minutes);
            int.TryParse(match.Groups["hour"].Value, out var hours);

            time = new TimeSpan(hours, minutes, 0);

            return match.Success;
        }

        static readonly Regex _rollRemainingRegex = new Regex(@"(?<remaining>\d+)\s+uses\s+left", _regexOptions);

        public bool TryParseRollRemaining(string s, out int count)
        {
            var match = _rollRemainingRegex.Match(s);

            int.TryParse(match.Groups["remaining"].Value, out count);

            return match.Success;
        }

        static readonly Regex _rollLimitedRegex = new Regex(@"roulette\s+is\s+limited", _regexOptions);

        public bool TryParseRollLimited(string s, out TimeSpan resetTime) => _rollLimitedRegex.IsMatch(s) & TryParseTime(s, out resetTime);

        static readonly Regex _claimSucceededRegex = new Regex(@"\*\*(?<claimer>.*)\*\*\s+and\s+\*\*(?<character>.*)\*\*.*married", _regexOptions);

        public bool TryParseClaimSucceeded(string s, out string claimer, out string claimed)
        {
            var match = _claimSucceededRegex.Match(s);

            claimer = match.Groups["claimer"].Value;
            claimed = match.Groups["character"].Value;

            return match.Success;
        }

        static readonly Regex _claimFailedRegex = new Regex(@"next\s+interval\s+begins", _regexOptions);

        public bool TryParseClaimFailed(string s, out TimeSpan resetTime) => _claimFailedRegex.IsMatch(s) & TryParseTime(s, out resetTime);

        static readonly Regex _kakeraSucceededRegex = new Regex(@":kakera\w?:\s*\*\*(?<claimer>.*)\s+\+(?<claimed>\d+)", _regexOptions);

        public bool TryParseKakeraSucceeded(string s, out string claimer, out int claimed)
        {
            var match = _kakeraSucceededRegex.Match(s);

            claimer = match.Groups["claimer"].Value;
            int.TryParse(match.Groups["claimed"].Value, out claimed);

            return match.Success;
        }

        static readonly Regex _kakeraFailedRegex = new Regex(@"can't\s+react\s+to\s+a\s+kakera", _regexOptions);

        public bool TryParseKakeraFailed(string s, out TimeSpan resetTime) => _kakeraFailedRegex.IsMatch(s) & TryParseTime(s, out resetTime);
    }
}