namespace Nysgjerrig
{
    public static class SlackExtensions
    {
        public static string ToSlackMention(this string id) => $"<@{id}>";
        public static string ToSlackUrl(this string url, string text) => $"<{url}|{text}>";
    }
}
