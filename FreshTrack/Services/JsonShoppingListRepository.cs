using System.Text.Json;

namespace FreshTrack;

public sealed class JsonShoppingListRepository : IShoppingListRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _filePath = Path.Combine(FileSystem.AppDataDirectory, "shopping-lists.json");

    public async Task<IReadOnlyList<ShoppingList>> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            if (!File.Exists(_filePath))
            {
                return Array.Empty<ShoppingList>();
            }

            await using var stream = File.OpenRead(_filePath);
            var lists = await JsonSerializer.DeserializeAsync<List<ShoppingList>>(stream, SerializerOptions, cancellationToken);
            var normalizedLists = lists ?? new List<ShoppingList>();
            NormalizeAndEnsureIds(normalizedLists);
            return normalizedLists;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(IEnumerable<ShoppingList> lists, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lists);

        var materializedLists = lists.ToList();
        NormalizeAndEnsureIds(materializedLists);

        var snapshot = materializedLists
            .Select(static list => list.Clone())
            .ToList();

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await _gate.WaitAsync(cancellationToken);

        try
        {
            await using var stream = File.Create(_filePath);
            await JsonSerializer.SerializeAsync(stream, snapshot, SerializerOptions, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static void NormalizeAndEnsureIds(IList<ShoppingList> lists)
    {
        var usedIds = new HashSet<int>();

        foreach (var list in lists)
        {
            NormalizeList(list);

            if (list.Id > 0 && usedIds.Add(list.Id))
            {
                continue;
            }

            list.Id = CreateUniqueId(usedIds);
        }
    }

    private static int CreateUniqueId(HashSet<int> usedIds)
    {
        for (var candidate = 1; candidate < int.MaxValue; candidate++)
        {
            if (usedIds.Add(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Unable to allocate a unique list id.");
    }

    private static ShoppingList NormalizeList(ShoppingList list)
    {
        var fallbackTime = DateTime.Now;

        list.Name ??= string.Empty;
        list.Vegetable ??= string.Empty;
        list.Meat ??= string.Empty;
        list.Drink ??= string.Empty;
        list.Item ??= string.Empty;
        list.Address ??= string.Empty;

        if (list.ReminderAt is DateTime reminderAt)
        {
            list.ReminderAt = ShoppingList.NormalizeReminderTime(reminderAt);
        }

        if (list.CreatedAt == default && list.LastUpdatedAt == default)
        {
            list.CreatedAt = fallbackTime;
            list.LastUpdatedAt = fallbackTime;
            return list;
        }

        if (list.CreatedAt == default)
        {
            list.CreatedAt = list.LastUpdatedAt;
        }

        if (list.LastUpdatedAt == default)
        {
            list.LastUpdatedAt = list.CreatedAt;
        }

        return list;
    }
}
