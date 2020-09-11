using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;

namespace Nysgjerrig
{
    public class SlackCronJob
    {
        private readonly Uri _queryNextPersonUri;

        public SlackCronJob()
        {
            var queryNextPersonEndpoint = Environment.GetEnvironmentVariable("QueryNextPersonEndpoint");

            if (string.IsNullOrWhiteSpace(queryNextPersonEndpoint)) throw new ArgumentException("***QueryNextPersonEndpoint not set***");

            _queryNextPersonUri = new Uri(queryNextPersonEndpoint);
        }

        [FunctionName("TimeTrigger")]
        public void TimeTrigger([TimerTrigger("0 0 11 * * 1-5")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"Time trigger fired: {DateTime.Now}");

            using var client = new HttpClient();

            client.GetAsync(_queryNextPersonUri);
        }
    }
}
