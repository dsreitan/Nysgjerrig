# Nysgjerrigbotten

Azure Function som spør om hverdagen til folk via Slack-meldinger. 

Opprett en `local.settings.json` på root:
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "IncludeBot": false,
    "QueryNextEndpoint": "https://####.azurewebsites.net/api/Next",
    "QueryFollowupEndpoint": "https://####.azurewebsites.net/api/Followup",
    "SlackBaseUrl": "https://slack.com/api",
    "SlackChannelId": "G01195YMUE9",
    "SlackBotId": "UGHP7FKBP",
    "SlackAccessTokenBot": "xoxb-####"
  }
}
```

[Hjelp til å finne riktig Cron-syntax](https://bradymholt.github.io/cron-expression-descriptor/?locale=en-US&expression=0+0+11+*+*+1-5) for å endre tidspunktene i triggerne. Standard alle hverdager kl. 11 GMT: `[TimerTrigger("0 0 11 * * 1-5")]`.
