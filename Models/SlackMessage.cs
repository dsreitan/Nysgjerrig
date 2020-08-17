namespace Nysgjerrig.Models
{
    public class SlackMessage
    {
        public string Type { get; set; }
        public string Subtype { get; set; }
        public string Text { get; set; }
        public string User { get; set; }
        public string BotId { get; set; }
        public string BotLink { get; set; }
        public string Ts { get; set; }
        public string Inviter { get; set; }
        public string ClientMsgId { get; set; }
        public string Team { get; set; }
    }
}
