using System.Collections.Generic;

namespace Nysgjerrig.Models
{
    // Slack classes

    public class SlackChannelMembersData
    {
        public bool Ok { get; set; }
        public IEnumerable<string> Members { get; set; }
    }
}
