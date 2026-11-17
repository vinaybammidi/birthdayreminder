# Birthday Reminder API

A simple ASP.NET Core Web API that reads birthday reminders from `data/reminders.json`,
finds today's birthdays, and fetches a fresh online greeting card on every greeting request.

## Run

```bash
dotnet run
```

The API prints its local URL. Open `/`, `/health`, or `/api/reminders/today/with-greeting`.

## API examples

```bash
# List reminders
curl http://localhost:5000/api/reminders

# Add a reminder
curl -X POST http://localhost:5000/api/reminders \
  -H 'Content-Type: application/json' \
  -d '{"name":"Grace Hopper","dateOfBirth":"1906-12-09","emailAddresses":["grace@example.com"]}'

# Get today's birthdays and a fresh greeting
curl http://localhost:5000/api/reminders/today/with-greeting
```

The file store creates `data/reminders.json` when it does not exist and writes updates
atomically. The greeting uses Quotable and falls back to a local message if the online
service is unavailable.
