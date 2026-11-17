using BirthdayReminder.Models;
using BirthdayReminder.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddHttpClient<GreetingCardService>(client =>
{
    client.BaseAddress = new Uri("https://api.quotable.io/");
    client.Timeout = TimeSpan.FromSeconds(5);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("BirthdayReminder/1.0");
});
builder.Services.AddSingleton<FileReminderStore>();
builder.Services.AddSingleton<ReminderService>();

var app = builder.Build();
app.UseExceptionHandler();

app.MapGet("/", () => Results.Ok(new
{
    name = "Birthday Reminder API",
    status = "running",
    endpoints = new[] { "/api/reminders", "/api/reminders/today", "/api/greeting", "/health" }
}));

app.MapGet("/health", (FileReminderStore store) => Results.Ok(new
{
    status = "healthy",
    reminderCount = store.GetAll().Count,
    utc = DateTimeOffset.UtcNow
}));

app.MapGet("/api/reminders", (ReminderService service) =>
    Results.Ok(service.GetAll()));

app.MapGet("/api/reminders/today", (ReminderService service) =>
    Results.Ok(service.GetToday()));

app.MapGet("/api/reminders/{id:int}", (int id, ReminderService service) =>
{
    var reminder = service.Get(id);
    return reminder is null ? Results.NotFound() : Results.Ok(reminder);
});

app.MapPost("/api/reminders", (CreateReminderRequest request, ReminderService service) =>
{
    var result = service.Create(request);
    return result.Error is not null
        ? Results.BadRequest(new { error = result.Error })
        : Results.Created($"/api/reminders/{result.Reminder!.Id}", result.Reminder);
});

app.MapPut("/api/reminders/{id:int}", (int id, UpdateReminderRequest request, ReminderService service) =>
{
    var result = service.Update(id, request);
    if (result.Error is not null)
    {
        return result.Error == "Reminder not found."
            ? Results.NotFound(new { error = result.Error })
            : Results.BadRequest(new { error = result.Error });
    }

    return Results.Ok(result.Reminder);
});

app.MapDelete("/api/reminders/{id:int}", (int id, ReminderService service) =>
    service.Delete(id) ? Results.NoContent() : Results.NotFound());

app.MapGet("/api/greeting", async (GreetingCardService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.GetFreshGreetingAsync(cancellationToken)));

app.MapGet("/api/reminders/today/with-greeting", async (
    ReminderService reminders,
    GreetingCardService greeting,
    CancellationToken cancellationToken) =>
{
    var card = await greeting.GetFreshGreetingAsync(cancellationToken);
    return Results.Ok(new { date = DateOnly.FromDateTime(DateTime.UtcNow), birthdays = reminders.GetToday(), greeting = card });
});

app.Run();
