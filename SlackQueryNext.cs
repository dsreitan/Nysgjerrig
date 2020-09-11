using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
            try
            {
                var commandRequest = req.HasFormContentType ?  new SlackCommandRequest(req.Form) : new SlackCommandRequest();

                log.LogInformation(JsonConvert.SerializeObject(commandRequest));

                var channelMembers = await GetChannelMemberIds();

                var channelMessages = await GetChannelMessages();

                // Forwarding spam prevention
                if (!string.IsNullOrWhiteSpace(commandRequest.UserName) && channelMessages.First().Text.Contains(commandRequest.UserName))
                {
                    return new OkObjectResult("Du kan ikke sende den videre flere ganger på rad :facepalm:");
                }

                var mentionHighscores = GetMentionHighscores(channelMembers, channelMessages);

                var chats = GetChats(mentionHighscores, commandRequest);

                var selectedChatIndex = new Random().Next(0, chats.Count());

                if (!string.IsNullOrWhiteSpace(commandRequest.Text))
                {
                    var mentionedMember = mentionHighscores.FirstOrDefault(x => commandRequest.Text.Contains(x.Id));
                    if (mentionedMember != null)
                    {
                        mentionedMember.Count = -1;
                        mentionHighscores = mentionHighscores.OrderBy(x => x.Count).ToList();
                    }
                    else if (int.TryParse(commandRequest.Text, out int chatNumber) && chatNumber > 0 && chatNumber <= chats.Count)
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
            catch (Exception ex)
            {
                log.LogInformation($"{ex.Message} \n{ex.StackTrace}");

                throw;
            }
        }
    }
}
