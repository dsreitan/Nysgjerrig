namespace Nysgjerrig.Models
{
    /// <summary>
    /// https://api.slack.com/types/user
    /// </summary>
    public class SlackUser
    {
        public string Id { get; set; }
        public string TeamId { get; set; }
        public string Name { get; set; }
        public bool Deleted { get; set; }
        public string Color { get; set; }
        public string RealName { get; set; }
        public string Tz { get; set; }
        public string TzLabel { get; set; }
        public int TzOffset { get; set; }
        public SlackProfile Profile { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsOwner { get; set; }
        public bool IsPrimaryOwner { get; set; }
        public bool IsRestricted { get; set; }
        public bool IsUltraRestricted { get; set; }
        public bool IsBot { get; set; }
        public bool IsAppUser { get; set; }
        public int Updated { get; set; }
        public string Locale { get; set; }
    }
}
