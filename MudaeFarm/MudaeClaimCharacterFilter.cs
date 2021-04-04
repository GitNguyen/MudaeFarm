using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace MudaeFarm
{
    public readonly struct CharacterInfo
    {
        static readonly Regex _bracketRegex = new Regex(@"(\(|\[).*(\)|\])", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        public readonly string Name;
        public readonly string Anime;

        public string DisplayName => _bracketRegex.Replace(Name, "").Trim();
        public string DisplayAnime => _bracketRegex.Replace(Anime, "").Trim();

        public CharacterInfo(string name, string anime)
        {
            Name  = name?.Trim() ?? "";
            Anime = anime?.Trim() ?? "";
        }

        public override string ToString() => $"{Name} ({Anime})";
    }

    public interface IMudaeClaimCharacterFilter
    {
        bool IsWished(CharacterInfo character, ulong[] wishedBy = null);
    }

    public class MudaeClaimCharacterFilter : IMudaeClaimCharacterFilter
    {
        readonly ILogger<MudaeClaimCharacterFilter> _logger;

        public MudaeClaimCharacterFilter(IOptionsMonitor<CharacterWishlist> characterWishlist, IOptionsMonitor<AnimeWishlist> animeWishlist, IOptionsMonitor<UserWishlistList> wishlistList, ILogger<MudaeClaimCharacterFilter> logger)
        {
            _logger = logger;

            ResetNameMatch(characterWishlist.CurrentValue);
            ResetAnimeMatch(animeWishlist.CurrentValue);
            ResetWishedByMatch(wishlistList.CurrentValue);

            characterWishlist.OnChange(ResetNameMatch);
            animeWishlist.OnChange(ResetAnimeMatch);
            wishlistList.OnChange(ResetWishedByMatch);
        }

        NameMatch _name;
        AnimeMatch _anime;
        WishedByMatch _wishedBy;

        void ResetNameMatch(CharacterWishlist wishlist)
        {
            try
            {
                _name = new NameMatch(wishlist);
                _logger.LogDebug($"Loaded character wishlist: {JsonConvert.SerializeObject(wishlist)}");
            }
            catch (Exception e)
            {
                _name = default;
                _logger.LogWarning(e, "Could not build character match.");
            }
        }

        void ResetAnimeMatch(AnimeWishlist wishlist)
        {
            try
            {
                _anime = new AnimeMatch(wishlist);
                _logger.LogDebug($"Loaded anime wishlist: {JsonConvert.SerializeObject(wishlist)}");
            }
            catch (Exception e)
            {
                _anime = default;
                _logger.LogWarning(e, "Could not build anime match.");
            }
        }

        void ResetWishedByMatch(UserWishlistList list)
        {
            try
            {
                _wishedBy = new WishedByMatch(list);
                _logger.LogDebug($"Loaded user wishlist list: {JsonConvert.SerializeObject(list)}");
            }
            catch (Exception e)
            {
                _wishedBy = default;
                _logger.LogWarning(e, "Could not build wishlist match.");
            }
        }

        readonly struct NameMatch
        {
            readonly Regex _name;

            public NameMatch(CharacterWishlist wishlist)
            {
                if (wishlist.Items.Count == 0)
                {
                    _name = null;
                    return;
                }

                var builder = new StringBuilder();
                var first   = true;

                foreach (var item in wishlist.Items)
                {
                    if (first)
                        first = false;
                    else
                        builder.Append('|');

                    builder.Append('(')
                           .Append(item.Name)
                           .Append(')');
                }

                _name = new Regex(builder.ToString(), RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
            }

            public bool IsMatch(CharacterInfo character) => _name?.IsMatch(character.Name ?? "") == true;
        }

        readonly struct AnimeMatch
        {
            readonly struct Item
            {
                readonly Regex _anime;
                readonly NameMatch _excluding;

                public Item(AnimeWishlist.Item item)
                {
                    _anime = new Regex(item.Name, RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

                    _excluding = item.Excluding == null
                        ? default
                        : new NameMatch(new CharacterWishlist { Items = new List<CharacterWishlist.Item>(item.Excluding) });
                }

                public bool IsMatch(CharacterInfo character) => _anime?.IsMatch(character.Anime ?? "") == true && !_excluding.IsMatch(character);
            }

            readonly Item[] _items;

            public AnimeMatch(AnimeWishlist wishlist)
            {
                _items = new Item[wishlist.Items.Count];

                for (var i = 0; i < _items.Length; i++)
                    _items[i] = new Item(wishlist.Items[i]);
            }

            public bool IsMatch(CharacterInfo character)
            {
                for (var i = 0; i < _items.Length; i++)
                {
                    if (_items[i].IsMatch(character))
                        return true;
                }

                return false;
            }
        }

        readonly struct WishedByMatch
        {
            readonly struct Item
            {
                readonly NameMatch _excludingCharacters;
                readonly AnimeMatch _excludingAnime;

                public Item(UserWishlistList.Item item)
                {
                    _excludingCharacters = item.ExcludingCharacters == null
                        ? default
                        : new NameMatch(new CharacterWishlist { Items = new List<CharacterWishlist.Item>(item.ExcludingCharacters) });

                    _excludingAnime = item.ExcludingAnime == null
                        ? default
                        : new AnimeMatch(new AnimeWishlist { Items = new List<AnimeWishlist.Item>(item.ExcludingAnime) });
                }

                public bool IsMatch(CharacterInfo character) => !_excludingCharacters.IsMatch(character) && !_excludingAnime.IsMatch(character);
            }

            readonly Dictionary<ulong, Item> _items;

            public WishedByMatch(UserWishlistList list)
            {
                _items = new Dictionary<ulong, Item>(list.Items.Count);

                foreach (var item in list.Items)
                    _items[item.Id] = new Item(item);
            }

            public bool IsMatch(CharacterInfo character, ulong[] wishedBy)
            {
                if (wishedBy == null)
                    return false;

                foreach (var userId in wishedBy)
                {
                    if (_items.TryGetValue(userId, out var item) && item.IsMatch(character))
                        return true;
                }

                return false;
            }
        }

        public bool IsWished(CharacterInfo character, ulong[] wishedBy = null) => _name.IsMatch(character) || _anime.IsMatch(character) || _wishedBy.IsMatch(character, wishedBy);
    }
}