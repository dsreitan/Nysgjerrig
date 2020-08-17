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
        private static readonly string SlackBaseUrl = "https://slack.com/api";
        private static readonly string SlackChannelId = Environment.GetEnvironmentVariable("SlackChannelId");
        private static readonly string SlackBotId = Environment.GetEnvironmentVariable("SlackBotId");
        private static readonly string SlackAccessTokenBot = Environment.GetEnvironmentVariable("SlackAccessTokenBot");
        private static readonly string EndpointToTrigger = Environment.GetEnvironmentVariable("EndpointToTrigger");
        private static readonly bool IncludeBot = bool.Parse(Environment.GetEnvironmentVariable("IncludeBot"));

        private static readonly HttpClient _httpClient = GetHttpClient();

        [FunctionName("TimeTrigger")]
        public static void TimeTrigger([TimerTrigger("0 0 11 * * 1-5")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"Time trigger fired: {DateTime.Now}");

            _httpClient.GetAsync(EndpointToTrigger);
        }

        [FunctionName("Test")]
        public static async Task<IActionResult> Test([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation($"Request method={req.Method} https={req.IsHttps} content-type={req.ContentType}");

            var channelMembers = await GetChannelMemberIds();

            var channelMessages = await GetChannelMessages();

            var allMessagesJoined = string.Join(null, channelMessages.Where(x => x.BotId == null).Select(x => x.Text));

            var membersHighscoreAsc = channelMembers
                .Select(x => new ChatMember { Id = x, Count = Regex.Matches(allMessagesJoined, x).Count })
                .OrderBy(x => x.Count);

            var response = "Antall mentions pr. bruker\n" + string.Join("\n", membersHighscoreAsc.Select(x => $"{x.Count:D2}: {x.Id}"));

            return new OkObjectResult(response);
        }

        [FunctionName("Next")]
        public static async Task<IActionResult> Next([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            //TODO: check if question already is sent this day to this user, return error string if so
            // add force-send-option

            log.LogInformation($"Request method={req.Method} https={req.IsHttps} content-type={req.ContentType}");

            // Slash Commands doc https://api.slack.com/interactivity/slash-commands
            var isSlashCommand = req.ContentType == "application/x-www-form-urlencoded" && req.Form.TryGetValue("text", out StringValues textValues) && !string.IsNullOrWhiteSpace(textValues);

            var channelMembers = await GetChannelMemberIds();

            var channelMessages = await GetChannelMessages(limit: 100);

            var allMessagesJoined = string.Join(null, channelMessages.Select(x => x.Text));

            var membersHighscoreAsc = channelMembers
                .Select(x => new ChatMember { Id = x, Count = Regex.Matches(allMessagesJoined, x).Count })
                .OrderBy(x => x.Count)
                .ToList();

            if (isSlashCommand)
            {
                var text = textValues.ToString();
                var mentionedMember = membersHighscoreAsc.FirstOrDefault(x => text.Contains(x.Id));
                if (mentionedMember != null)
                {
                    mentionedMember.Count = -1;
                    membersHighscoreAsc = membersHighscoreAsc.OrderBy(x => x.Count).ToList();
                }
            }

            var randomIndex = new Random().Next(0, Chats.Count());
            var randomChat = await Chats.ElementAt(randomIndex)(membersHighscoreAsc);

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

        private static readonly List<Func<List<ChatMember>, Task<Chat>>> Chats = new List<Func<List<ChatMember>, Task<Chat>>>
        {
            async (List<ChatMember> members) => new Chat {
                Question = $"Hei {members.First().Id.ToSlackMention()}! Hva jobber du med i dag? 🖨",
                Reminder = "Eller har du fri?"
            },
            async (List<ChatMember> members) => new Chat{ Question = $"God dag {members.First().Id.ToSlackMention()}! Gjør du noe gøy akkurat nå? :ninja:"},
            async (List<ChatMember> members) => new Chat{ Question = $"Hallo {members.First().Id.ToSlackMention()}! Hva skjer? :what: :up:"},
            async (List<ChatMember> members) =>
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
            async (List<ChatMember> members) =>
            {
                var question = $"Kjeder du deg {members.First().Id.ToSlackMention()}?";
                var feed = await _httpClient.GetStringAsync("https://www.kode24.no/?lab_viewport=rss");
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
            async (List<ChatMember> members) => new Chat{ Question = $"Har det blitt noe <#CRJ7QDS90> i det siste {members.First().Id.ToSlackMention()}? :microphone:"},

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

        /// <summary>
        /// https://api.slack.com/methods/users.info
        /// </summary>
        private static async Task<SlackUser> GetSlackUserInfo(string id)
        {
            var userInfoResponse = await _httpClient.GetAsync($"{SlackBaseUrl}/users.info?token={SlackAccessTokenBot}&user={id}");
            var userInfoData = await userInfoResponse.Content.ReadAsAsync<SlackUserInfoData>();
            return userInfoData.Ok ? userInfoData.User : null;
        }

        /// <summary>
        /// https://api.slack.com/methods/users.profile.get
        /// </summary>
        private static async Task<SlackProfile> GetSlackUserProfile(string id)
        {
            var userProfileResponse = await _httpClient.GetAsync($"{SlackBaseUrl}/users.profile.get?token={SlackAccessTokenBot}&user={id}");
            var userProfileData = await userProfileResponse.Content.ReadAsAsync<SlackUserProfileData>();
            return userProfileData.Ok ? userProfileData.Profile : null;
        }

        /// <summary>
        /// https://api.slack.com/methods/chat.postMessage
        /// </summary>
        private static async Task SendMessage(Chat chat)
        {
            var token = SlackAccessTokenBot;
            var channel = SlackChannelId;
            var text = chat.Question;

            var json = JsonConvert.SerializeObject(new { token, channel, text });
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            await _httpClient.PostAsync($"{SlackBaseUrl}/chat.postMessage", data);
        }

        /// <summary>
        /// https://api.slack.com/methods/conversations.members
        /// </summary>
        private static async Task<IEnumerable<string>> GetChannelMemberIds(bool randomOrder = true)
        {
            var channelMembersResponse = await _httpClient.GetAsync($"{SlackBaseUrl}/conversations.members?token={SlackAccessTokenBot}&channel={SlackChannelId}");
            var channelMembersData = await channelMembersResponse.Content.ReadAsAsync<SlackChannelMembersData>();

            if (channelMembersData?.Ok != true) return null;

            var channelMemberIds = channelMembersData.Members.Where(x => IncludeBot || x != SlackBotId);

            if (randomOrder) channelMemberIds = channelMemberIds.OrderBy(x => Guid.NewGuid());

            return channelMemberIds;
        }

        /// <summary>
        /// https://api.slack.com/methods/conversations.history
        /// </summary>
        private static async Task<IEnumerable<SlackMessage>> GetChannelMessages(int limit = 100)
        {
            var channelHistoryResponse = await _httpClient.GetAsync($"{SlackBaseUrl}/conversations.history?token={SlackAccessTokenBot}&channel={SlackChannelId}&limit={limit}");
            var channelHistoryData = await channelHistoryResponse.Content.ReadAsAsync<SlackChannelHistoryData>();

            return channelHistoryData?.Ok == true ? channelHistoryData.Messages : null;
        }


        private static HttpClient GetHttpClient()
        {
            var httpClient = new HttpClient { BaseAddress = new Uri(SlackBaseUrl) };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SlackAccessTokenBot);
            return httpClient;
        }
    }
}
