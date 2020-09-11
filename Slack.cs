using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Toolkit.Parsers.Rss;
using Nysgjerrig.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Nysgjerrig
{
    class Slack : SlackBase
    {
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
    }
}
