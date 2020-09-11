using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Nysgjerrig.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Nysgjerrig
{
    class SlackQuestNext : SlackBase
    {
        [FunctionName("Next")]
        public async Task<IActionResult> Next([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation($"Request method={req.Method} https={req.IsHttps} content-type={req.ContentType}");

            var commandRequest = new SlackCommandRequest(req.Form, "/neste");

            var channelMembers = await GetChannelMemberIds();

            var channelMessages = await GetChannelMessages();

            // Forwarding spam prevention
            if (!string.IsNullOrWhiteSpace(commandRequest.UserName) && channelMessages.LastOrDefault(x => x.Text.Contains(commandRequest.UserName)) != null)
            {
                return new OkObjectResult("Du kan ikke sende den videre flere ganger på rad :facepalm:");
            }

            var mentionHighscores = GetMentionHighscores(channelMembers, channelMessages);

            var chats = GetChats(mentionHighscores, commandRequest);

            var selectedChatIndex = new Random().Next(0, chats.Count());

            if (!string.IsNullOrWhiteSpace(commandRequest.CommandValue))
            {
                var mentionedMember = mentionHighscores.FirstOrDefault(x => commandRequest.CommandValue.Contains(x.Id));
                if (mentionedMember != null)
                {
                    mentionedMember.Count = -1;
                    mentionHighscores = mentionHighscores.OrderBy(x => x.Count).ToList();
                }
                else if (int.TryParse(commandRequest.CommandValue, out int chatNumber) && chatNumber > 0 && chatNumber <= chats.Count)
                {
                    selectedChatIndex = chatNumber - 1;
                }
            }

            var selectedChat = await chats.ElementAt(selectedChatIndex)();

            await SendMessage(selectedChat, commandRequest);

            /*
            * optional:
            * check for replies after two minutes
            * - if none, remind
            * - if only selected user, send randomized positive response
            * - if other users responded as well, react
            */

            return new OkObjectResult(null);
        }
    }
}
