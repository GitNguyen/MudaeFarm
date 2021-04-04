using System;
using System.Collections.Generic;
using Disqord;
using Microsoft.Extensions.Options;

namespace MudaeFarm
{
    public interface IMudaeClaimEmojiFilter
    {
        bool IsClaimEmoji(IEmoji emoji);
        bool IsKakeraEmoji(IEmoji emoji, out KakeraType kakera);
    }

    public class MudaeClaimEmojiFilter : IMudaeClaimEmojiFilter
    {
        readonly IOptionsMonitor<ClaimingOptions> _options;

        public MudaeClaimEmojiFilter(IOptionsMonitor<ClaimingOptions> options)
        {
            _options = options;
        }

        // https://emojipedia.org/hearts/
        static readonly string[] _heartEmojis =
        {
            "\uD83D\uDC98", // cupid
            "\uD83D\uDC9D", // gift_heart
            "\uD83D\uDC96", // sparkling_heart
            "\uD83D\uDC97", // heartpulse
            "\uD83D\uDC93", // heartbeat
            "\uD83D\uDC9E", // revolving_hearts
            "\uD83D\uDC95", // two_hearts
            "\uD83D\uDC9F", // heart_decoration
            "\u2764",       // heart
            "\uD83E\uDDE1", // heart (orange)
            "\uD83D\uDC9B", // yellow_heart
            "\uD83D\uDC9A", // green_heart
            "\uD83D\uDC99", // blue_heart
            "\uD83D\uDC9C", // purple_heart
            "\uD83E\uDD0E", // heart (brown)
            "\uD83D\uDDA4", // heart (black)
            "\uD83E\uDD0D", // heart (white)
            "\u2665"        // hearts
        };

        public bool IsClaimEmoji(IEmoji emoji)
        {
            if (_options.CurrentValue.CustomEmotes)
                return true;

            var name = emoji.Name;

            // remove variation selectors
            name = name.Replace("\uFE0E", "")
                       .Replace("\uFE0F", "");

            return Array.IndexOf(_heartEmojis, name) != -1;
        }

        static readonly Dictionary<string, KakeraType> _kakeraMap = new Dictionary<string, KakeraType>(StringComparer.OrdinalIgnoreCase)
        {
            { "kakerap", KakeraType.Purple },
            { "kakera", KakeraType.Blue },
            { "kakerat", KakeraType.Teal },
            { "kakerag", KakeraType.Green },
            { "kakeray", KakeraType.Yellow },
            { "kakerao", KakeraType.Orange },
            { "kakerar", KakeraType.Red },
            { "kakeraw", KakeraType.Rainbow },
            { "kakeral", KakeraType.Light }
        };

        public bool IsKakeraEmoji(IEmoji emoji, out KakeraType kakera) => emoji is ICustomEmoji & _kakeraMap.TryGetValue(emoji.Name, out kakera);
    }
}