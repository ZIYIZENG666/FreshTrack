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
            return (lists ?? new List<ShoppingList>())
                .Select(NormalizeList)
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(IEnumerable<ShoppingList> lists, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(lists);

        var snapshot = lists
            .Select(static list => new ShoppingList
            {
                Id = list.Id,
                Name = list.Name,
                Vegetable = list.Vegetable,
                Meat = list.Meat,
                Drink = list.Drink,
                Item = list.Item,
                Address = list.Address,
                CreatedAt = list.CreatedAt,
                LastUpdatedAt = list.LastUpdatedAt
            })
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

    private static ShoppingList NormalizeList(ShoppingList list)
    {
        var fallbackTime = DateTime.Now;

        list.Name ??= string.Empty;
        list.Vegetable ??= string.Empty;
        list.Meat ??= string.Empty;
        list.Drink ??= string.Empty;
        list.Item ??= string.Empty;
        list.Address ??= string.Empty;

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
