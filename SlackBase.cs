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
    class SlackBase
    {
        internal const int MessageLimit = 100;
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

        internal List<ChatMember> GetMentionHighscores(IEnumerable<string> channelMembers, IEnumerable<SlackMessage> channelMessages)
        {
            var allMessagesJoined = string.Join(null, channelMessages.Select(x => x.Text));

            return channelMembers
                .Select(x => new ChatMember { Id = x, Count = Regex.Matches(allMessagesJoined, x).Count })
                .OrderBy(x => x.Count)
                .ThenBy(x => Guid.NewGuid())
                .ToList();
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
        internal async Task SendMessage(Chat chat, SlackCommandRequest commandRequest)
        {
            var token = SlackAccessTokenBot;
            var channel = SlackChannelId;
            var text = chat.Question;
            if (!string.IsNullOrWhiteSpace(commandRequest.UserName))
            {
                text = $"{text}\n(videresendt fra {commandRequest.UserName} :incoming_envelope:)";
            }

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
            if (!channelMembersResponse.IsSuccessStatusCode)
            {
                throw new Exception($"conversations.members failed: {channelMembersResponse.StatusCode} {channelMembersResponse.ReasonPhrase}");
            }

            var channelMembersData = await channelMembersResponse.Content.ReadAsAsync<SlackChannelMembersData>();

            return channelMembersData.Ok ? channelMembersData.Members.Where(x => IncludeBot || x != SlackBotId) : null;
        }

        /// <summary>
        /// https://api.slack.com/methods/conversations.history
        /// </summary>
        internal async Task<IEnumerable<SlackMessage>> GetChannelMessages(int limit = MessageLimit)
        {
            var channelHistoryResponse = await HttpClient.GetAsync($"{SlackBaseUrl}/conversations.history?token={SlackAccessTokenBot}&channel={SlackChannelId}&limit={limit}");
            var channelHistoryData = await channelHistoryResponse.Content.ReadAsAsync<SlackChannelHistoryData>();

            return channelHistoryData.Ok ? channelHistoryData.Messages : null;
        }

        /// <summary>
        /// https://api.slack.com/methods/reactions.add
        /// </summary>
        internal async Task<bool> ReactToMessage(string timestamp, string name = "thumbsup")
        {
            var token = SlackAccessTokenBot;
            var channel = SlackChannelId;

            var json = JsonConvert.SerializeObject(new { token, channel, name, timestamp });
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await HttpClient.PostAsync($"{SlackBaseUrl}/reactions.add", data);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"reactions.add failed: {response.StatusCode} {response.ReasonPhrase}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var contentJson = JsonConvert.DeserializeObject<dynamic>(responseContent);

            return bool.TryParse($"{contentJson.ok}", out bool isOk) && isOk;
        }

        internal List<Func<Task<Chat>>> GetChats(List<ChatMember> members, SlackCommandRequest commandRequest = null)
        {
            return new List<Func<Task<Chat>>>
            {
                async () => {
                    var workEmojies = new []{ ":floppy_disk:", ":printer:", ":chart_with_upwards_trend:", ":briefcase:"};
                    return new Chat{ Question = $"Hei {members.First().Id.ToSlackMention()}! Hva jobber du med i dag? {workEmojies[new Random().Next(0,workEmojies.Length)]}"};
                },
                async () => {
                    var funEmojies = new []{ ":bowling:", ":video_game:", ":ice_skate:", ":woman-surfing:"};
                    return new Chat{ Question = $"God dag {members.First().Id.ToSlackMention()}! Gjør du noe gøy akkurat nå? {funEmojies[new Random().Next(0,funEmojies.Length)]}"};
                },
                async () => new Chat{ Question = $"Hallo {members.First().Id.ToSlackMention()}! Hva skjer? :what: :up:"},
                async () =>
                {
                    var leastMentionedUserId = members.First().Id;

                    var question = $"Hva driver du med {leastMentionedUserId.ToSlackMention()}? ☺";

                    var mostMentionedUser = await GetSlackUserInfo(members.Last().Id);
                    if (mostMentionedUser?.Name != null)
                    {
                        var mostMentionedUserName = mostMentionedUser.Name.Split(".")[0].Capitalize();
                        question = $"Nå har jeg plaga {mostMentionedUserName} nok! " + question;
                    }

                    return new Chat { Question = question};
                },
                async () =>
                {
                    var question = $"Fortell oss hva du jobber med {members.First().Id.ToSlackMention()}!";
                    var feed = await HttpClient.GetStringAsync("https://www.kode24.no/?lab_viewport=rss");
                    if (feed != null)
                    {
                        var parser = new RssParser();
                        var rss = parser.Parse(feed);
                        var latestPostUrl = rss.First()?.FeedUrl?.Replace("\n", "").Trim();
                        if (latestPostUrl != null)
                        {
                            question += $" Etterpå kan du jo sjekke {latestPostUrl.ToSlackUrl("siste nytt på kode24")} :moonwalk-parrot:";
                        }
                    }

                    return new Chat { Question = question};
                },
                // CRJ7QDS90 == #karaoke channel
                async () => new Chat { Question = $"Har det blitt noe {"CRJ7QDS90".ToSlackChannel()} i det siste {members.First().Id.ToSlackMention()}? :microphone: Eller noe annet som skjer?"},
                async () =>
                {
                    var question = $"Kompilerer ikke koden {members.First().Id.ToSlackMention()}? Du kan jo lufte problemet her";

                    try
                    {
                        var officeLocations = new Dictionary<string, string>
                        {
                            {"Oslo", "lat=59.91273&lon=10.74609" },
                            {"Eidsvoll", "lat=60.33045&lon=11.26161" },
                            {"Ulsteinvik", "lat=62.34316&lon=5.84867" },
                            {"Karmøy", "lat=59.28354&lon=5.30671" },
                            {"København", "lat=55.67594&lon=12.56553" },
                            {"Trysil", "lat=61.31485&lon=12.2637" },
                            {"Fredrikstad", "lat=59.2263353&lon=10.8800742" },
                            {"Moss", "lat=59.428299&lon=10.667990" },                          
                        };

                        var randomOffice = officeLocations.ElementAt(new Random().Next(0, officeLocations.Count));

                        using var client = new HttpClient();
                        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("min-slack-integrasjon", "0.0.1"));

                        var url = "https://api.met.no/weatherapi/locationforecast/2.0/compact.json?" + randomOffice.Value;
                        var weatherResponse = await client.GetAsync(url);
                        var weatherData = await weatherResponse.Content.ReadAsStringAsync();
                        var weatherJson = JsonConvert.DeserializeObject<dynamic>(weatherData);
                        if (weatherJson != null)
                        {
                            question += $" eller lufte hjernen ute! Her på {randomOffice.Key}-kontoret er det {GetWeatherDescription(weatherJson)}";
                        }
                    }
                    catch (Exception)
                    {
                        question += " :male-technologist:";
                    }


                    return new Chat{ Question = question};
                },
                async () => new Chat{ Question = $"Sitter du fast i et problem {members.First().Id.ToSlackMention()}? La oss hjelpe deg! :rubber_duck:"},

                // TODO
                
                // REMINDERS
                // po
                // cv 
                // forecast + navn

                // REPLIES - if other replies  react, else message
            };
        }

        /// <summary>
        /// https://api.met.no/weatherapi/locationforecast/2.0/compact.json
        /// </summary>
        private string GetWeatherDescription(dynamic weatherJson)
        {
            var timeseriesNow = weatherJson.properties.timeseries[2];
            var tempereature = timeseriesNow.data.instant.details.air_temperature;
            var symbolCode = $"{timeseriesNow.data.next_1_hours.summary.symbol_code}";

            return $"{tempereature} grader og {GetWeatherEmoji(symbolCode)}";
        }

        private string GetWeatherEmoji(string symbolCode)
        {
            if (symbolCode.Contains("rain")) return ":rain_cloud:";
            if (symbolCode.Contains("sun")) return ":sunny:";
            if (symbolCode.Contains("cloud")) return ":cloud:";
            return ":partly_sunny:";
        }
    }
}
