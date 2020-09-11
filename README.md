# Nysgjerrigbotten

Azure Function som spør om hverdagen til folk via Slack-meldinger. 

1. Endre tidspunktene for når botten spør via Cron-jobbene: `[TimerTrigger("0 0 11 * * 1-5")]`. [Hjelp til å finne riktig syntax](https://bradymholt.github.io/cron-expression-descriptor/?locale=en-US&expression=0+0+11+*+*+1-5).

2. Opprett en `local.settings.json` på root:
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "IncludeBot": false,
    "QueryNextPersonEndpoint": "https://####.azurewebsites.net/api/Next",
    "QueryFollowupEndpoint": "https://####.azurewebsites.net/api/Followup",
    "SlackBaseUrl": "https://slack.com/api",
    "SlackChannelId": "G01195YMUE9",
    "SlackBotId": "UGHP7FKBP",
    "SlackAccessTokenBot": "xoxb-####"
  }
}
```
