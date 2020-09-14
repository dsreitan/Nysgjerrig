using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Nysgjerrig
{
    class SlackQueryFollowup : SlackBase
    {
        [FunctionName("Followup")]
        public async Task<IActionResult> Followup([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            try
            {
                log.LogInformation($"Request method={req.Method} https={req.IsHttps} content-type={req.ContentType}");

                var channelMessages = await GetChannelMessages(limit: 1);

                var lastMessage = channelMessages.Last();

                if (lastMessage.User == SlackBotId)
                {
                    await ReactToMessage(lastMessage.Ts, "question");
                }
                else
                {
                    await ReactToMessage(lastMessage.Ts);
                }

                return new OkObjectResult(null);
            }
            catch (Exception ex)
            {
                log.LogInformation($"{ex.Message} \n{ex.StackTrace}");

                throw;
            }

            //TODO: Find a way to fetch the previous question to send an appropriate answer/reminder
            //var fakeMembers = new List<ChatMember> { new ChatMember { Id = ""} };
            //var chats = GetChats(fakeMembers);
            //await SendMessage(selectedChat, commandRequest);

            /*
            * optional:
            * check for replies after two minutes
            * - if none, remind
            * - if only selected user, send randomized positive response
            * - if other users responded as well, react
            */
        }
    }
}
