namespace Disqord
{
    public sealed class EmbedThumbnail
    {
        public string Url { get; internal set; }

        public string ProxyUrl { get; internal set; }

        public int? Height { get; internal set; }

        public int? Width { get; internal set; }

        internal EmbedThumbnail()
        { }
    }
}
