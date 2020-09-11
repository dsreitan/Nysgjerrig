using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Toolkit.Parsers.Rss;
using Newtonsoft.Json;
using Nysgjerrig.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nysgjerrig
{
    public class Slack
    {
        public Slack()
        {
            SlackBaseUrl = "https://slack.com/api";
            SlackChannelId = Environment.GetEnvironmentVariable("SlackChannelId");
            SlackBotId = Environment.GetEnvironmentVariable("SlackBotId");
            SlackAccessTokenBot = Environment.GetEnvironmentVariable("SlackAccessTokenBot");
            IncludeBot = bool.TryParse(Environment.GetEnvironmentVariable("IncludeBot"), out bool value) && value;

            if (string.IsNullOrWhiteSpace(SlackChannelId)) throw new ArgumentException("***SlackChannelId not set***");
            if (string.IsNullOrWhiteSpace(SlackBotId)) throw new ArgumentException("***SlackBotId not set***");
            if (string.IsNullOrWhiteSpace(SlackAccessTokenBot)) throw new ArgumentException("***SlackAccessTokenBot not set***");

            HttpClient = new HttpClient { BaseAddress = new Uri(SlackBaseUrl) };
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SlackAccessTokenBot);
        }

        public string SlackBaseUrl { get; set; }
        public string SlackChannelId { get; set; }
        public string SlackBotId { get; set; }
        public string SlackAccessTokenBot { get; set; }
        public bool IncludeBot { get; set; }
        public HttpClient HttpClient { get; set; }

        [FunctionName("Test")]
        public async Task<IActionResult> Test([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation($"Request method={req.Method} https={req.IsHttps} content-type={req.ContentType}");

            try
            {
                var channelMembers = await GetChannelMemberIds();

                var channelMessages = await GetChannelMessages();

                var mentionHighscores = GetMentionHighscores(channelMembers, channelMessages);

                var response = "Antall mentions pr. bruker\n" + string.Join("\n", mentionHighscores.Select(x => $"{x.Count:D2}: {x.Id}"));

                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                log.LogInformation($"{ex.Message} \n{ex.StackTrace}");

                throw;
            }
        }

        [FunctionName("Next")]
        public async Task<IActionResult> Next([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            //TODO: check if question already is sent this day to this user, return error string if so
            // add force-send-option

            log.LogInformation($"Request method={req.Method} https={req.IsHttps} content-type={req.ContentType}");

            // Slash Commands doc https://api.slack.com/interactivity/slash-commands
            var isSlashCommand = req.ContentType == "application/x-www-form-urlencoded" && req.Form.TryGetValue("text", out StringValues textValues) && !string.IsNullOrWhiteSpace(textValues);

            var channelMembers = await GetChannelMemberIds();

            var channelMessages = await GetChannelMessages(limit: 100);

            var mentionHighscores = GetMentionHighscores(channelMembers, channelMessages).ToList();

            if (isSlashCommand)
            {
                var text = textValues.ToString();
                var mentionedMember = mentionHighscores.FirstOrDefault(x => text.Contains(x.Id));
                if (mentionedMember != null)
                {
                    mentionedMember.Count = -1;
                    mentionHighscores = mentionHighscores.OrderBy(x => x.Count).ToList();
                }
            }

            var chats = GetChats(mentionHighscores);
            var randomIndex = new Random().Next(0, chats.Count());
            var randomChat = await chats.ElementAt(randomIndex)();

            await SendMessage(randomChat);

            /*
            * optional:
            * check for replies after two minutes
            * - if none, remind
            * - if only selected user, send randomized positive response
            * - if other users responded as well, react
            */

            return new OkObjectResult(null);
        }

        private List<Func<Task<Chat>>> GetChats(List<ChatMember> members)
        {
            return new List<Func<Task<Chat>>>
            {
                async () => new Chat {
                    Question = $"Hei {members.First().Id.ToSlackMention()}! Hva jobber du med i dag? 🖨",
                    Reminder = "Eller har du fri?"
                },
                async () => new Chat{ Question = $"God dag {members.First().Id.ToSlackMention()}! Gjør du noe gøy akkurat nå? :ninja:"},
                async () => new Chat{ Question = $"Hallo {members.First().Id.ToSlackMention()}! Hva skjer? :what: :up:"},
                async () =>
                {
                    var mostMentionedUser = await GetSlackUserInfo(members.Last().Id);//await GetSlackUserProfile(members.Last().Id); Can't get profile for some reason..
                    string mostMentionedUserName;
                    if (string.IsNullOrWhiteSpace(mostMentionedUser?.Name))
                    {
                        mostMentionedUserName = members.Last().Id.ToSlackMention();
                    }
                    else
                    {
                        var capitalized = mostMentionedUser.Name.First().ToString().ToUpper() + mostMentionedUser.Name.Substring(1);
                        mostMentionedUserName = capitalized.Contains(".") ? capitalized.Split(".")[0] : capitalized;
                    }

                    var leastMentionedUserId = members.First().Id;

                    return new Chat{ Question = $"Nå har det vært mye fra {mostMentionedUserName} her! Jeg vil heller høre hva du driver med {leastMentionedUserId.ToSlackMention()} ☺"};
                },
                async () =>
                {
                    var question = $"Kjeder du deg {members.First().Id.ToSlackMention()}?";
                    var feed = await HttpClient.GetStringAsync("https://www.kode24.no/?lab_viewport=rss");
                    if (feed != null)
                    {
                        var parser = new RssParser();
                        var rss = parser.Parse(feed);
                        var latestPostUrl = rss.First()?.FeedUrl?.Replace("\n", "").Trim();
                        if (latestPostUrl != null)
                        {
                            question += $" Sjekk ut {latestPostUrl.ToSlackUrl("siste nytt på kode24")}!";
                        }
                    }

                    return new Chat{ Question = question};
                },
                async () => new Chat{ Question = $"Har det blitt noe {"CRJ7QDS90".ToSlackChannel()} i det siste {members.First().Id.ToSlackMention()}? :microphone:"},

                // QUESTIONS
                // weather api - it's nice weather here in oslo, how about where you are X?

                // REMINDERS
                // po
                // cv 
                // forecast + navn

                // REPLIES - if other replies react, else message
                // https://api.slack.com/methods/reactions.add
                // 
            };
        }

        private IEnumerable<ChatMember> GetMentionHighscores(IEnumerable<string> channelMembers, IEnumerable<SlackMessage> channelMessages)
        {
            var allMessagesJoined = string.Join(null, channelMessages.Select(x => x.Text));

            return channelMembers
                .Select(x => new ChatMember { Id = x, Count = Regex.Matches(allMessagesJoined, x).Count })
                .OrderBy(x => x.Count)
                .ThenBy(x => Guid.NewGuid());
        }

        /// <summary>
        /// https://api.slack.com/methods/users.info
        /// </summary>
        private async Task<SlackUser> GetSlackUserInfo(string id)
        {
            var userInfoResponse = await HttpClient.GetAsync($"{SlackBaseUrl}/users.info?token={SlackAccessTokenBot}&user={id}");
            var userInfoData = await userInfoResponse.Content.ReadAsAsync<SlackUserInfoData>();
            return userInfoData.Ok ? userInfoData.User : null;
        }

        /// <summary>
        /// https://api.slack.com/methods/users.profile.get
        /// </summary>
        private async Task<SlackProfile> GetSlackUserProfile(string id)
        {
            var userProfileResponse = await HttpClient.GetAsync($"{SlackBaseUrl}/users.profile.get?token={SlackAccessTokenBot}&user={id}");
            var userProfileData = await userProfileResponse.Content.ReadAsAsync<SlackUserProfileData>();
            return userProfileData.Ok ? userProfileData.Profile : null;
        }

        /// <summary>
        /// https://api.slack.com/methods/chat.postMessage
        /// </summary>
        private async Task SendMessage(Chat chat)
        {
            var token = SlackAccessTokenBot;
            var channel = SlackChannelId;
            var text = chat.Question;

            var json = JsonConvert.SerializeObject(new { token, channel, text });
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            await HttpClient.PostAsync($"{SlackBaseUrl}/chat.postMessage", data);
        }

        /// <summary>
        /// https://api.slack.com/methods/conversations.members
        /// </summary>
        private async Task<IEnumerable<string>> GetChannelMemberIds()
        {
            var channelMembersResponse = await HttpClient.GetAsync($"{SlackBaseUrl}/conversations.members?token={SlackAccessTokenBot}&channel={SlackChannelId}");
            if (!channelMembersResponse.IsSuccessStatusCode) throw new Exception($"conversations.members failed with status {channelMembersResponse.StatusCode} {channelMembersResponse.ReasonPhrase}");

            var channelMembersData = await channelMembersResponse.Content.ReadAsAsync<SlackChannelMembersData>();
            if (channelMembersData?.Ok != true || channelMembersData.Members?.Any() != true) throw new Exception($"conversations.members failed in channel {SlackChannelId}");

            return channelMembersData.Members.Where(x => IncludeBot || x != SlackBotId);
        }

        /// <summary>
        /// https://api.slack.com/methods/conversations.history
        /// </summary>
        private async Task<IEnumerable<SlackMessage>> GetChannelMessages(int limit = 100)
        {
            var channelHistoryResponse = await HttpClient.GetAsync($"{SlackBaseUrl}/conversations.history?token={SlackAccessTokenBot}&channel={SlackChannelId}&limit={limit}");
            var channelHistoryData = await channelHistoryResponse.Content.ReadAsAsync<SlackChannelHistoryData>();

            return channelHistoryData?.Ok == true ? channelHistoryData.Messages : null;
        }
    }
}
