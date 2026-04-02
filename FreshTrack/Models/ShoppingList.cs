using System.Text.Json.Serialization;

namespace FreshTrack;

public class ShoppingList
{
    public int Id { get; set; } = Random.Shared.Next(1000, 9999);
    public string Name { get; set; } = string.Empty;
    public string Vegetable { get; set; } = string.Empty;
    public string Meat { get; set; } = string.Empty;
    public string Drink { get; set; } = string.Empty;
    public string Item { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }

    [JsonIgnore]
    public string BadgeText
    {
        get
        {
            var trimmedName = Name?.Trim();
            if (!string.IsNullOrWhiteSpace(trimmedName))
            {
                return trimmedName[..1].ToUpperInvariant();
            }

            return "L";
        }
    }

    [JsonIgnore]
    public string CardTimestamp =>
        LastUpdatedAt == default
            ? "Recently updated"
            : LastUpdatedAt.ToString("yyyy-MM-dd HH:mm");
}
