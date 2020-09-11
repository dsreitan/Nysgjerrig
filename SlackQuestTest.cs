using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Nysgjerrig.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nysgjerrig
{
    class SlackQuestTest : SlackBase
    {
        [FunctionName("Test")]
        public async Task<IActionResult> Test([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation($"Request method={req.Method} https={req.IsHttps} content-type={req.ContentType}");

            var commandRequest = new SlackCommandRequest(req.Form);

            try
            {
                var channelMembers = await GetChannelMemberIds();

                var channelMessages = await GetChannelMessages();

                var mentionHighscores = GetMentionHighscores(channelMembers, channelMessages);

                var lowestMentionCount = mentionHighscores.First().Count;

                var mentionsTable = $"\n\nAntall ganger en bruker er nevnt de siste {MessageLimit} meldingene i {SlackChannelId.ToSlackChannel()}. En av de uthevede radene blir valgt ut neste gang.\n" + string.Join("\n", mentionHighscores.Select(x =>
                {
                    var row = $"{x.Count:D2}: {x.Id}";
                    if (commandRequest.UserId == x.Id) row = $"_{row}_";
                    if (lowestMentionCount == x.Count) row = $"*{row}*";
                    return row;
                }));

                var responseText =
                    $"Hei {commandRequest.UserName}, din ID på Slack er {commandRequest.UserId}." +
                    mentionsTable +
                    await GetChatPreview(commandRequest, mentionHighscores);

                return new OkObjectResult(responseText);
            }
            catch (Exception ex)
            {
                log.LogInformation($"{ex.Message} \n{ex.StackTrace}");

                throw;
            }
        }

        private async Task<string> GetChatPreview(SlackCommandRequest commandRequest, List<ChatMember> mentionHighscores)
        {
            var requestedChatText = "";

            if (!string.IsNullOrWhiteSpace(commandRequest.CommandValue))
            {
                if (!int.TryParse(commandRequest.CommandValue, out int chatNumber) || chatNumber <= 0)
                {
                    requestedChatText = "\n\nUgyldig verdi på valg av spørsmål.";
                }
                else
                {
                    var chats = GetChats(mentionHighscores, commandRequest);
                    if (chats.Count < chatNumber)
                    {
                        requestedChatText = $"\n\nDet er kun {chats.Count} spørsmål tilgjengelig.";
                    }
                    else
                    {
                        var chatIndex = chatNumber - 1;
                        var requestedChat = await chats.ElementAt(chatIndex)();
                        requestedChatText = $"\n\nSpørsmål #{chatNumber}\n{requestedChat.Question}";
                    }
                }
            }

            return requestedChatText;
        }
    }
}
