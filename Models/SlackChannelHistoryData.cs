using System.Collections.Generic;

namespace Nysgjerrig.Models
{
    public class SlackChannelHistoryData
    {
        public bool Ok { get; set; }
        public IList<SlackMessage> Messages { get; set; }
        public bool HasMore { get; set; }
        public int PinCount { get; set; }
        public object ChannelActionsTs { get; set; }
        public int ChannelActionsCount { get; set; }
    }
}
