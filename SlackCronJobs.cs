using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Nysgjerrig
{
    public class SlackCronJobs
    {
        private readonly string _queryNextUrl;
        private readonly string _queryFollowupUrl;

        public SlackCronJobs()
        {
            _queryNextUrl = Environment.GetEnvironmentVariable("QueryNextEndpoint");
            _queryFollowupUrl = Environment.GetEnvironmentVariable("QueryFollowupEndpoint");

            if (string.IsNullOrWhiteSpace(_queryNextUrl)) throw new ArgumentException("***QueryNextEndpoint not set***");
            if (string.IsNullOrWhiteSpace(_queryFollowupUrl)) throw new ArgumentException("***QueryFollowupEndpoint not set***");
        }

        [FunctionName("TriggerNext")]
        public async Task TriggerNext([TimerTrigger("0 0 11 * * 1-5")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"{nameof(TriggerNext)} fired: {DateTime.Now}");

            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync(_queryNextUrl);
                if (response.IsSuccessStatusCode)
                {
                    log.LogInformation($"{nameof(TriggerNext)} OK {await response.Content.ReadAsStringAsync()}");
                }
                else
                {
                    log.LogWarning($"{nameof(TriggerNext)} FAILED {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message, ex);
                throw;
            }
        }

        [FunctionName("TriggerFollowup")]
        public async Task TriggerFollowup([TimerTrigger("0 15 11 * * 1-5")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"{nameof(TriggerFollowup)} fired: {DateTime.Now}");

            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync(_queryFollowupUrl);
                if (response.IsSuccessStatusCode)
                {
                    log.LogInformation($"{nameof(TriggerFollowup)} OK {await response.Content.ReadAsStringAsync()}");
                }
                else
                {
                    log.LogWarning($"{nameof(TriggerFollowup)} FAILED {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message, ex);
                throw;
            }
        }
    }
}
