# Nysgjerrigbotten

Azure Function som spør om hverdagen til folk via Slackmeldinger.

Opprett en `local.settings.json` på root.
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "EndpointToTrigger": "https://####.azurewebsites.net/api/Next",
    "IncludeBot": "False",
    "SlackChannelId": "G01195YMUE9",
    "SlackBotId": "UGHP7FKBP",
    "SlackAccessTokenBot": "xoxb-####"
  }
}
```
