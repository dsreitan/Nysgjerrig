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
    class SlackBase
    {
        internal readonly string SlackBaseUrl;
        internal readonly string SlackChannelId;
        internal readonly string SlackBotId;
        internal readonly string SlackAccessTokenBot;
        internal readonly bool IncludeBot;
        internal readonly HttpClient HttpClient;

        internal SlackBase()
        {
            SlackBaseUrl = Environment.GetEnvironmentVariable("SlackBaseUrl");
            SlackChannelId = Environment.GetEnvironmentVariable("SlackChannelId");
            SlackBotId = Environment.GetEnvironmentVariable("SlackBotId");
            SlackAccessTokenBot = Environment.GetEnvironmentVariable("SlackAccessTokenBot");
            IncludeBot = bool.TryParse(Environment.GetEnvironmentVariable("IncludeBot"), out bool value) && value;

            if (string.IsNullOrWhiteSpace(SlackBaseUrl)) throw new ArgumentException("***SlackBaseUrl not set***");
            if (string.IsNullOrWhiteSpace(SlackChannelId)) throw new ArgumentException("***SlackChannelId not set***");
            if (string.IsNullOrWhiteSpace(SlackBotId)) throw new ArgumentException("***SlackBotId not set***");
            if (string.IsNullOrWhiteSpace(SlackAccessTokenBot)) throw new ArgumentException("***SlackAccessTokenBot not set***");

            HttpClient = new HttpClient { BaseAddress = new Uri(SlackBaseUrl) };
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SlackAccessTokenBot);
        }

        internal IEnumerable<ChatMember> GetMentionHighscores(IEnumerable<string> channelMembers, IEnumerable<SlackMessage> channelMessages)
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
        internal async Task<SlackUser> GetSlackUserInfo(string id)
        {
            var userInfoResponse = await HttpClient.GetAsync($"{SlackBaseUrl}/users.info?token={SlackAccessTokenBot}&user={id}");
            var userInfoData = await userInfoResponse.Content.ReadAsAsync<SlackUserInfoData>();
            return userInfoData.Ok ? userInfoData.User : null;
        }

        /// <summary>
        /// https://api.slack.com/methods/users.profile.get
        /// </summary>
        internal async Task<SlackProfile> GetSlackUserProfile(string id)
        {
            var userProfileResponse = await HttpClient.GetAsync($"{SlackBaseUrl}/users.profile.get?token={SlackAccessTokenBot}&user={id}");
            var userProfileData = await userProfileResponse.Content.ReadAsAsync<SlackUserProfileData>();
            return userProfileData.Ok ? userProfileData.Profile : null;
        }

        /// <summary>
        /// https://api.slack.com/methods/chat.postMessage
        /// </summary>
        internal async Task SendMessage(Chat chat)
        {
            var token = SlackAccessTokenBot;
            var channel = SlackChannelId;
            var text = chat.Question;

            var json = JsonConvert.SerializeObject(new { token, channel, text });
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{SlackBaseUrl}/chat.postMessage", data);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"chat.postMessage failed: {response.StatusCode} {response.ReasonPhrase}");
            }
        }

        /// <summary>
        /// https://api.slack.com/methods/conversations.members
        /// </summary>
        internal async Task<IEnumerable<string>> GetChannelMemberIds()
        {
            var channelMembersResponse = await HttpClient.GetAsync($"{SlackBaseUrl}/conversations.members?token={SlackAccessTokenBot}&channel={SlackChannelId}");
            if (!channelMembersResponse.IsSuccessStatusCode) throw new Exception($"conversations.members failed: {channelMembersResponse.StatusCode} {channelMembersResponse.ReasonPhrase}");

            var channelMembersData = await channelMembersResponse.Content.ReadAsAsync<SlackChannelMembersData>();
            if (channelMembersData?.Ok != true || channelMembersData.Members?.Any() != true) throw new Exception($"conversations.members failed in channel {SlackChannelId}");

            return channelMembersData.Members.Where(x => IncludeBot || x != SlackBotId);
        }

        /// <summary>
        /// https://api.slack.com/methods/conversations.history
        /// </summary>
        internal async Task<IEnumerable<SlackMessage>> GetChannelMessages(int limit = 100)
        {
            var channelHistoryResponse = await HttpClient.GetAsync($"{SlackBaseUrl}/conversations.history?token={SlackAccessTokenBot}&channel={SlackChannelId}&limit={limit}");
            var channelHistoryData = await channelHistoryResponse.Content.ReadAsAsync<SlackChannelHistoryData>();

            return channelHistoryData.Ok ? channelHistoryData.Messages : null;
        }
    }
}
