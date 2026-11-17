using System.Net.Mail;
using BirthdayReminder.Models;

namespace BirthdayReminder.Services;

public sealed class ReminderService(FileReminderStore store)
{
    public IReadOnlyList<ReminderRecord> GetAll() => store.GetAll();

    public ReminderRecord? Get(int id) => store.Get(id);

    public IReadOnlyList<ReminderRecord> GetToday()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return store.GetAll().Where(x => x.DateOfBirth.Month == today.Month && x.DateOfBirth.Day == today.Day).ToArray();
    }

    public (ReminderRecord? Reminder, string? Error) Create(CreateReminderRequest request)
    {
        var error = Validate(request.Name, request.EmailAddresses);
        if (error is not null)
        {
            return (null, error);
        }

        var nextId = store.GetAll().Select(x => x.Id).DefaultIfEmpty(0).Max() + 1;
        var reminder = new ReminderRecord(nextId, request.Name!.Trim(), request.DateOfBirth, NormalizeEmails(request.EmailAddresses!));
        return (store.Add(reminder), null);
    }

    public (ReminderRecord? Reminder, string? Error) Update(int id, UpdateReminderRequest request)
    {
        if (store.Get(id) is null)
        {
            return (null, "Reminder not found.");
        }

        var error = Validate(request.Name, request.EmailAddresses);
        if (error is not null)
        {
            return (null, error);
        }

        var reminder = new ReminderRecord(id, request.Name!.Trim(), request.DateOfBirth, NormalizeEmails(request.EmailAddresses!));
        return (store.Replace(reminder) ? reminder : null, null);
    }

    public bool Delete(int id) => store.Delete(id);

    private static string? Validate(string? name, IReadOnlyList<string>? emails)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Name is required.";
        }

        if (emails is null || emails.Count == 0)
        {
            return "At least one email address is required.";
        }

        if (emails.Any(email => !IsEmail(email)))
        {
            return "All email addresses must be valid.";
        }

        return null;
    }

    private static bool IsEmail(string email)
    {
        try
        {
            return new MailAddress(email).Address == email.Trim();
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static IReadOnlyList<string> NormalizeEmails(IEnumerable<string> emails) =>
        emails.Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
}
