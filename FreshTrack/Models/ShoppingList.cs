using System.Text.Json.Serialization;

namespace FreshTrack;

public class ShoppingList
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Vegetable { get; set; } = string.Empty;
    public string Meat { get; set; } = string.Empty;
    public string Drink { get; set; } = string.Empty;
    public string Item { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime? ReminderAt { get; set; }
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
        ReminderAt is DateTime reminderAt
            ? NormalizeReminderTime(reminderAt).ToString("yyyy-MM-dd HH:mm")
            : "No reminder set";

    public static DateTime NormalizeReminderTime(DateTime reminderAt)
    {
        var localReminder = reminderAt.Kind switch
        {
            DateTimeKind.Utc => reminderAt.ToLocalTime(),
            DateTimeKind.Local => reminderAt,
            _ => DateTime.SpecifyKind(reminderAt, DateTimeKind.Local)
        };

        return new DateTime(
            localReminder.Year,
            localReminder.Month,
            localReminder.Day,
            localReminder.Hour,
            localReminder.Minute,
            0,
            DateTimeKind.Local);
    }

    public ShoppingList Clone()
    {
        return new ShoppingList
        {
            Id = Id,
            Name = Name,
            Vegetable = Vegetable,
            Meat = Meat,
            Drink = Drink,
            Item = Item,
            Address = Address,
            ReminderAt = ReminderAt,
            CreatedAt = CreatedAt,
            LastUpdatedAt = LastUpdatedAt
        };
    }
}
