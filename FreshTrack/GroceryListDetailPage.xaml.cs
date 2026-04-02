namespace FreshTrack;

public partial class GroceryListDetailPage : ContentPage
{
    private const string DefaultReminderListName = "Grocery List";
    private const string ReminderMessage = "It's time to check your grocery list!";

    private ShoppingList? _workingList;
    private Func<ShoppingList, Task>? _onSave;
    private readonly IShoppingListRepository _shoppingListRepository;
    private bool _isNew;
    private bool _closeAfterSave;
    private bool _hasInitializedStandalone;
    private string _originalVegetable = string.Empty;
    private string _originalMeat = string.Empty;
    private string _originalDrink = string.Empty;
    private string _originalItem = string.Empty;
    private string _originalAddress = string.Empty;
    private string _originalName = string.Empty;

    public GroceryListDetailPage()
    {
        InitializeComponent();
        _shoppingListRepository = ServiceHelper.GetRequiredService<IShoppingListRepository>();
    }

    public GroceryListDetailPage(ShoppingList list, Func<ShoppingList, Task> onSave) : this()
    {
        InitializePage(list, onSave, isNew: false, closeAfterSave: true);
    }

    public GroceryListDetailPage(Func<ShoppingList, Task> onSave) : this()
    {
        InitializePage(new ShoppingList(), onSave, isNew: true, closeAfterSave: true);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_hasInitializedStandalone || _workingList is not null)
        {
            return;
        }

        InitializePage(new ShoppingList(), SaveStandaloneListAsync, isNew: true, closeAfterSave: false);
        _hasInitializedStandalone = true;
    }

    private void InitializePage(ShoppingList list, Func<ShoppingList, Task> onSave, bool isNew, bool closeAfterSave)
    {
        _workingList = CloneList(list);
        _onSave = onSave;
        _isNew = isNew;
        _closeAfterSave = closeAfterSave;
        BindingContext = _workingList;

        NameEntry.Text = _workingList.Name;
        VegetableEntry.Text = _workingList.Vegetable;
        MeatEntry.Text = _workingList.Meat;
        DrinkEntry.Text = _workingList.Drink;
        ItemEntry.Text = _workingList.Item;
        AddressEntry.Text = _workingList.Address;

        CaptureOriginalValues(list);
        SaveButton.IsVisible = isNew;
        BackButton.IsVisible = closeAfterSave;
    }

    private static ShoppingList CloneList(ShoppingList list)
    {
        return new ShoppingList
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
        };
    }

    private void CaptureOriginalValues(ShoppingList list)
    {
        _originalName = list.Name;
        _originalVegetable = list.Vegetable;
        _originalMeat = list.Meat;
        _originalDrink = list.Drink;
        _originalItem = list.Item;
        _originalAddress = list.Address;
    }

    private async void OnClockClicked(object? sender, EventArgs e)
    {
        var reminderService = GetReminderService();
        if (reminderService is null)
        {
            await DisplayAlertAsync("Error", "Reminder service not available", "OK");
            return;
        }

        if (!await reminderService.RequestNotificationPermissionAsync())
        {
            await DisplayAlertAsync("Permission Required", "Please grant notification permission to use reminders", "OK");
            return;
        }

        var selectedTime = await PromptForReminderTimeAsync();
        if (selectedTime is null)
        {
            return;
        }

        var triggerTime = BuildTriggerTime(selectedTime.Value);
        var reminderTitle = $"Reminder: {GetReminderListName()}";
        var listId = _workingList?.Id ?? Random.Shared.Next(1000, 9999);

        reminderService.SetReminder(triggerTime, reminderTitle, ReminderMessage, listId);
        await DisplayAlertAsync("Success", $"Reminder set for {triggerTime:HH:mm}", "OK");
    }

    private async Task<TimeSpan?> PromptForReminderTimeAsync()
    {
        var timeInput = await DisplayPromptAsync(
            "Set Reminder Time",
            "Enter time in HH:mm format (e.g., 14:30)",
            placeholder: "HH:mm",
            initialValue: DateTime.Now.ToString("HH:mm"));

        if (string.IsNullOrWhiteSpace(timeInput))
        {
            return null;
        }

        if (TimeSpan.TryParse(timeInput, out var selectedTime))
        {
            return selectedTime;
        }

        await DisplayAlertAsync("Invalid Time", "Please enter a valid time (HH:mm)", "OK");
        return null;
    }

    private static DateTime BuildTriggerTime(TimeSpan selectedTime)
    {
        var currentTime = DateTime.Now;
        var triggerTime = new DateTime(
            currentTime.Year,
            currentTime.Month,
            currentTime.Day,
            selectedTime.Hours,
            selectedTime.Minutes,
            selectedTime.Seconds);

        if (triggerTime < currentTime)
        {
            triggerTime = triggerTime.AddDays(1);
        }

        return triggerTime;
    }

    private string GetReminderListName()
    {
        var listName = NameEntry?.Text?.Trim();
        return string.IsNullOrWhiteSpace(listName) ? DefaultReminderListName : listName;
    }

    private IReminderService? GetReminderService()
    {
        return ServiceHelper.GetService<IReminderService>();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        if (!_closeAfterSave)
        {
            await Shell.Current.GoToAsync($"//{AppRoutes.Lists}");
            return;
        }

        try
        {
            await Shell.Current.GoToAsync("..");
        }
        catch
        {
            if (Navigation.NavigationStack.Count > 1)
            {
                await Navigation.PopAsync();
                return;
            }

            await Shell.Current.GoToAsync($"//{AppRoutes.Lists}");
        }
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (_workingList is null || _onSave is null)
        {
            return;
        }

        var nameInput = NameEntry?.Text?.Trim();
        var vegetable = VegetableEntry?.Text?.Trim() ?? string.Empty;
        var meat = MeatEntry?.Text?.Trim() ?? string.Empty;
        var drink = DrinkEntry?.Text?.Trim() ?? string.Empty;
        var item = ItemEntry?.Text?.Trim() ?? string.Empty;
        var address = AddressEntry?.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(vegetable)
            && string.IsNullOrWhiteSpace(meat)
            && string.IsNullOrWhiteSpace(drink)
            && string.IsNullOrWhiteSpace(item))
        {
            await DisplayAlertAsync("Notice", "Please enter at least one item in Vegetable, Meat, Drink, or Item.", "OK");
            return;
        }

        ApplyInputs(_workingList, nameInput, vegetable, meat, drink, item, address);
        PrepareTimestampsForSave(_workingList);
        SaveButton.IsEnabled = false;

        try
        {
            await _onSave(_workingList);
            CaptureOriginalValues(_workingList);
            _isNew = false;
            SaveButton.IsVisible = false;

            if (_closeAfterSave)
            {
                await Navigation.PopAsync();
                return;
            }

            await DisplayAlertAsync("Saved", $"'{_workingList.Name}' has been saved.", "OK");
        }
        catch
        {
            await DisplayAlertAsync("Save Failed", "Unable to save this list right now.", "OK");
        }
        finally
        {
            SaveButton.IsEnabled = true;
        }
    }

    private static void ApplyInputs(ShoppingList list, string? nameInput, string vegetable, string meat, string drink, string item, string address)
    {
        list.Vegetable = vegetable;
        list.Meat = meat;
        list.Drink = drink;
        list.Item = item;
        list.Address = address;

        if (!string.IsNullOrWhiteSpace(nameInput))
        {
            list.Name = nameInput;
            return;
        }

        if (string.IsNullOrWhiteSpace(list.Name))
        {
            list.Name = CreateFallbackName(vegetable, meat, drink, item);
        }
    }

    private static string CreateFallbackName(string vegetable, string meat, string drink, string item)
    {
        var parts = new[] { vegetable, meat, drink, item }
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Take(2)
            .ToArray();

        return parts.Length switch
        {
            0 => "Fresh List",
            1 => parts[0],
            _ => $"{parts[0]} & {parts[1]}"
        };
    }

    private static void PrepareTimestampsForSave(ShoppingList list)
    {
        var now = DateTime.Now;

        if (list.CreatedAt == default)
        {
            list.CreatedAt = now;
        }

        list.LastUpdatedAt = now;
    }

    private void OnContentChanged(object? sender, TextChangedEventArgs e)
    {
        SaveButton.IsVisible = _isNew || HasContentChanged();
    }

    private bool HasContentChanged()
    {
        var name = NameEntry?.Text ?? string.Empty;
        var vegetable = VegetableEntry?.Text ?? string.Empty;
        var meat = MeatEntry?.Text ?? string.Empty;
        var drink = DrinkEntry?.Text ?? string.Empty;
        var item = ItemEntry?.Text ?? string.Empty;
        var address = AddressEntry?.Text ?? string.Empty;

        return name != _originalName
            || vegetable != _originalVegetable
            || meat != _originalMeat
            || drink != _originalDrink
            || item != _originalItem
            || address != _originalAddress;
    }

    private async Task SaveStandaloneListAsync(ShoppingList saved)
    {
        var existingLists = (await _shoppingListRepository.LoadAsync()).ToList();
        var existingIndex = existingLists.FindIndex(list => list.Id == saved.Id);

        if (existingIndex >= 0)
        {
            existingLists[existingIndex] = CloneList(saved);
        }
        else
        {
            existingLists.Add(CloneList(saved));
        }

        await _shoppingListRepository.SaveAsync(existingLists);
    }
}
