using System.Text.Json;
using BirthdayReminder.Models;

namespace BirthdayReminder.Services;

public sealed class FileReminderStore
{
    private readonly string _filePath;
    private readonly object _sync = new();
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private List<ReminderRecord>? _cache;

    public FileReminderStore(IHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "data");
        Directory.CreateDirectory(dataDirectory);
        _filePath = Path.Combine(dataDirectory, "reminders.json");
    }

    public IReadOnlyList<ReminderRecord> GetAll()
    {
        lock (_sync)
        {
            EnsureLoaded();
            return _cache!.OrderBy(x => x.Name).ToArray();
        }
    }

    public ReminderRecord? Get(int id)
    {
        lock (_sync)
        {
            EnsureLoaded();
            return _cache!.FirstOrDefault(x => x.Id == id);
        }
    }

    public ReminderRecord Add(ReminderRecord reminder)
    {
        lock (_sync)
        {
            EnsureLoaded();
            _cache!.Add(reminder);
            Save();
            return reminder;
        }
    }

    public bool Replace(ReminderRecord reminder)
    {
        lock (_sync)
        {
            EnsureLoaded();
            var index = _cache!.FindIndex(x => x.Id == reminder.Id);
            if (index < 0)
            {
                return false;
            }

            _cache[index] = reminder;
            Save();
            return true;
        }
    }

    public bool Delete(int id)
    {
        lock (_sync)
        {
            EnsureLoaded();
            var removed = _cache!.RemoveAll(x => x.Id == id) > 0;
            if (removed)
            {
                Save();
            }

            return removed;
        }
    }

    private void EnsureLoaded()
    {
        if (_cache is not null)
        {
            return;
        }

        if (!File.Exists(_filePath))
        {
            _cache = [];
            Save();
            return;
        }

        var json = File.ReadAllText(_filePath);
        _cache = JsonSerializer.Deserialize<List<ReminderRecord>>(json, _jsonOptions) ?? [];
    }

    private void Save()
    {
        var temporaryPath = $"{_filePath}.tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(_cache, _jsonOptions));
        File.Move(temporaryPath, _filePath, true);
    }
}
