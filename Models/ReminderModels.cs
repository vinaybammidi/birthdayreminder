namespace BirthdayReminder.Models;

public sealed record ReminderRecord(
    int Id,
    string Name,
    DateOnly DateOfBirth,
    IReadOnlyList<string> EmailAddresses);

public sealed record CreateReminderRequest(
    string? Name,
    DateOnly DateOfBirth,
    IReadOnlyList<string>? EmailAddresses);

public sealed record UpdateReminderRequest(
    string? Name,
    DateOnly DateOfBirth,
    IReadOnlyList<string>? EmailAddresses);

public sealed record GreetingCard(
    string Text,
    string Author,
    string Source,
    DateTimeOffset RetrievedAt);
