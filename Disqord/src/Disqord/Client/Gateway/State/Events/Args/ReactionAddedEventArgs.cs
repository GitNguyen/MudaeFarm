﻿namespace Disqord.Events
{
    public sealed class ReactionAddedEventArgs : DiscordEventArgs
    {
        public ICachedMessageChannel Channel { get; }

        public FetchableSnowflakeOptional<IMessage> Message { get; }

        public FetchableSnowflakeOptional<IUser> User { get; }

        public Optional<ReactionData> Reaction { get; }

        public IEmoji Emoji { get; }

        internal ReactionAddedEventArgs(
            ICachedMessageChannel channel,
            FetchableSnowflakeOptional<IMessage> message,
            FetchableSnowflakeOptional<IUser> user,
            Optional<ReactionData> reaction,
            IEmoji emoji) : base(channel.Client)
        {
            Channel = channel;
            Message = message;
            User = user;
            Reaction = reaction;
            Emoji = emoji;
        }
    }
}
