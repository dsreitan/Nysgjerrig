using Microsoft.AspNetCore.Http;

namespace Nysgjerrig.Models
{
    /// <summary>
    /// https://api.slack.com/interactivity/slash-commands#app_command_handling
    /// </summary>
    public class SlackCommandRequest
    {
        public SlackCommandRequest() { }
        public SlackCommandRequest(IFormCollection form)
        {
            Command = form["command"];
            Text = form["text"];
            ResponseUrl = form["response_url"];
            TriggerId = form["trigger_id"];
            UserId = form["user_id"];
            UserName = form["user_name"];
            TeamId = form["team_id"];
            EnterpriseId = form["enterprise_id"];
            ChannelId = form["channel_id"];
            ApiAppId = form["api_app_id"];
        }

        public string Command { get; set; }
        public string Text { get; set; }
        public string ResponseUrl { get; set; }
        public string TriggerId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string TeamId { get; set; }
        public string EnterpriseId { get; set; }
        public string ChannelId { get; set; }
        public string ApiAppId { get; set; }
    }
}
