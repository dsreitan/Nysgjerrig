namespace Nysgjerrig
{
    public static class SlackExtensions
    {
        public static string ToSlackMention(this string userId) => $"<@{userId}>";
        public static string ToSlackChannel(this string channelId) => $"<#{channelId}>";
        public static string ToSlackUrl(this string url, string text) => $"<{url}|{text}>";
    }
}
