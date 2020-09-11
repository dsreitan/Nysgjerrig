using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;

namespace Nysgjerrig
{
    public class SlackCronJobs
    {
        private readonly Uri _queryNextPersonUri;
        private readonly Uri _queryFollowup;

        public SlackCronJobs()
        {
            var queryNextPersonEndpoint = Environment.GetEnvironmentVariable("QueryNextPersonEndpoint");
            var queryFollowupEndpoint = Environment.GetEnvironmentVariable("QueryFollowupEndpoint");

            if (string.IsNullOrWhiteSpace(queryNextPersonEndpoint)) throw new ArgumentException("***QueryNextPersonEndpoint not set***");
            if (string.IsNullOrWhiteSpace(queryFollowupEndpoint)) throw new ArgumentException("***QueryFollowupEndpoint not set***");

            _queryNextPersonUri = new Uri(queryNextPersonEndpoint);
            _queryFollowup = new Uri(queryFollowupEndpoint);
        }

        [FunctionName("TriggerNextPerson")]
        public void TriggerNextPerson([TimerTrigger("0 0 11 * * 1-5")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"{nameof(TriggerNextPerson)} fired: {DateTime.Now}");

            using var client = new HttpClient();

            client.GetAsync(_queryNextPersonUri);
        }

        [FunctionName("TriggerFollowup")]
        public void TriggerFollowup([TimerTrigger("0 15 11 * * 1-5")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"{nameof(TriggerFollowup)} fired: {DateTime.Now}");

            using var client = new HttpClient();

            client.GetAsync(_queryFollowup);
        }
    }
}
