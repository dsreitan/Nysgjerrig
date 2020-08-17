namespace Nysgjerrig.Models
{
    public class SlackProfile
    {
        public string Title { get; set; }
        public string Phone { get; set; }
        public string Skype { get; set; }
        public string RealName { get; set; }
        public string RealNameNormalized { get; set; }
        public string DisplayName { get; set; }
        public string DisplayNameNormalized { get; set; }
        public object Fields { get; set; }
        public string StatusText { get; set; }
        public string StatusEmoji { get; set; }
        public int StatusExpiration { get; set; }
        public string AvatarHash { get; set; }
        public string ImageOriginal { get; set; }
        public bool IsCustomImage { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Image24 { get; set; }
        public string Image32 { get; set; }
        public string Image48 { get; set; }
        public string Image72 { get; set; }
        public string Image192 { get; set; }
        public string Image512 { get; set; }
        public string Image1024 { get; set; }
        public string StatusTextCanonical { get; set; }
        public string Team { get; set; }
    }
}
