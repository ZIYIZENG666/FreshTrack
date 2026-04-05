using System.Security.Cryptography;

namespace FreshTrack;

public partial class GroceryListDetailPage : ContentPage
{
    private const string DefaultReminderListName = "Grocery List";
    private const string ReminderMessage = "It's time to check your grocery list!";
    private static readonly TimeSpan DefaultReminderLeadTime = TimeSpan.FromMinutes(30);

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
    private DateTime? _originalReminderAt;
    private bool _originalHasReminder;
    private bool _hasReminder;
    private bool _isInitializing;
    private readonly int _temporaryReminderId = RandomNumberGenerator.GetInt32(1, int.MaxValue);

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
        _isInitializing = true;
        _workingList = list.Clone();
        _onSave = onSave;
        _isNew = isNew;
        _closeAfterSave = closeAfterSave;
        _hasReminder = list.ReminderAt is not null;
        BindingContext = _workingList;

        NameEntry.Text = _workingList.Name;
        VegetableEntry.Text = _workingList.Vegetable;
        MeatEntry.Text = _workingList.Meat;
        DrinkEntry.Text = _workingList.Drink;
        ItemEntry.Text = _workingList.Item;
        AddressEntry.Text = _workingList.Address;
        UpdateReminderDisplay();

        CaptureOriginalValues(_workingList);
        BackButton.IsVisible = closeAfterSave;
        _isInitializing = false;
        UpdateSaveState();
    }

    private void CaptureOriginalValues(ShoppingList list)
    {
        _originalName = list.Name;
        _originalVegetable = list.Vegetable;
        _originalMeat = list.Meat;
        _originalDrink = list.Drink;
        _originalItem = list.Item;
        _originalAddress = list.Address;
        _originalHasReminder = _hasReminder;
        _originalReminderAt = _hasReminder ? NormalizeReminder(list.ReminderAt) : null;
    }

    private void OnClockClicked(object? sender, EventArgs e)
    {
        InitializeReminderInputs(_workingList?.ReminderAt);
        ReminderSelectionOverlay.IsVisible = true;
    }

    private void InitializeReminderInputs(DateTime? reminderAt)
    {
        if (ReminderDatePicker is null || ReminderTimePicker is null)
        {
            return;
        }

        var defaultReminder = reminderAt is DateTime existingReminder
            ? ShoppingList.NormalizeReminderTime(existingReminder)
            : DateTime.Now.Add(DefaultReminderLeadTime);
        if (defaultReminder < DateTime.Now)
        {
            defaultReminder = DateTime.Now.Add(DefaultReminderLeadTime);
        }

        ReminderDatePicker.MinimumDate = DateTime.Today;
        ReminderDatePicker.Date = defaultReminder.Date;
        ReminderTimePicker.Time = defaultReminder.TimeOfDay;
    }

    private void UpdateReminderDisplay()
    {
        if (ReminderDateDisplayLabel is null || ReminderTimeDisplayLabel is null || ReminderStatusLabel is null)
        {
            return;
        }

        if (_workingList?.ReminderAt is not DateTime reminderAt)
        {
            ReminderDateDisplayLabel.Text = "Not set";
            ReminderTimeDisplayLabel.Text = "--:--";
            ReminderStatusLabel.Text = "Tap Reminder to set the schedule.";
            return;
        }

        var localReminder = ShoppingList.NormalizeReminderTime(reminderAt);
        ReminderDateDisplayLabel.Text = localReminder.ToString("yyyy-MM-dd");
        ReminderTimeDisplayLabel.Text = localReminder.ToString("HH:mm");
        ReminderStatusLabel.Text = "Tap Reminder to change the schedule.";
    }

    private async Task<DateTime?> BuildTriggerTime()
    {
        if (ReminderDatePicker is null || ReminderTimePicker is null)
        {
            return null;
        }

        var selectedDate = ReminderDatePicker.Date;
        if (selectedDate is null)
        {
            await DisplayAlertAsync("Invalid Date", "Please choose a valid reminder date.", "OK");
            return null;
        }

        var selectedTime = ReminderTimePicker.Time;
        if (selectedTime is null)
        {
            await DisplayAlertAsync("Invalid Time", "Please choose a valid reminder time.", "OK");
            return null;
        }

        var triggerTime = DateTime.SpecifyKind(
            selectedDate.Value.Date.Add(selectedTime.Value),
            DateTimeKind.Local);
        if (triggerTime <= DateTime.Now)
        {
            await DisplayAlertAsync("Invalid Time", "Please choose a future date and time for the reminder.", "OK");
            return null;
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
        var reminderChanged = HasReminderChanged();
        DateTime? reminderTimeToSave = _workingList.ReminderAt;

        if (string.IsNullOrWhiteSpace(vegetable)
            && string.IsNullOrWhiteSpace(meat)
            && string.IsNullOrWhiteSpace(drink)
            && string.IsNullOrWhiteSpace(item))
        {
            await DisplayAlertAsync("Notice", "Please enter at least one item in Vegetable, Meat, Drink, or Item.", "OK");
            return;
        }

        if (reminderChanged)
        {
            reminderTimeToSave = await BuildTriggerTime();
            if (reminderTimeToSave is null)
            {
                return;
            }
        }

        ApplyInputs(_workingList, nameInput, vegetable, meat, drink, item, address);
        _workingList.ReminderAt = _hasReminder ? reminderTimeToSave : null;
        PrepareTimestampsForSave(_workingList);
        SaveButton.IsEnabled = false;

        try
        {
            await _onSave(_workingList);
            var reminderUpdated = await ApplyReminderIfNeededAsync(_workingList, reminderChanged);
            CaptureOriginalValues(_workingList);
            _isNew = false;
            UpdateSaveState();

            if (_closeAfterSave)
            {
                if (!reminderUpdated)
                {
                    await DisplayAlertAsync("Reminder Not Updated", "The list was saved, but the reminder could not be updated on this device.", "OK");
                }

                await Navigation.PopAsync();
                return;
            }

            var saveMessage = reminderUpdated
                ? $"'{_workingList.Name}' has been saved."
                : $"'{_workingList.Name}' has been saved, but the reminder could not be updated on this device.";
            await DisplayAlertAsync("Saved", saveMessage, "OK");
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
        UpdateSaveState();
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

    private bool HasReminderChanged()
    {
        if (_originalHasReminder != _hasReminder)
        {
            return true;
        }

        if (!_hasReminder)
        {
            return false;
        }

        return NormalizeReminder(_originalReminderAt) != NormalizeReminder(_workingList?.ReminderAt);
    }

    private static DateTime? NormalizeReminder(DateTime? reminderAt)
    {
        return reminderAt is DateTime value
            ? ShoppingList.NormalizeReminderTime(value)
            : null;
    }

    private async Task<bool> ApplyReminderIfNeededAsync(ShoppingList list, bool reminderChanged)
    {
        if (!reminderChanged)
        {
            return true;
        }

        var reminderService = GetReminderService();
        if (reminderService is null)
        {
            return false;
        }

        if (!await reminderService.RequestNotificationPermissionAsync())
        {
            return false;
        }

        var listId = list.Id > 0 ? list.Id : _temporaryReminderId;
        reminderService.CancelReminder(listId);

        if (list.ReminderAt is not DateTime reminderAt)
        {
            return true;
        }

        var reminderTitle = $"Reminder: {GetReminderListName()}";
        reminderService.SetReminder(reminderAt, reminderTitle, ReminderMessage, listId);
        return true;
    }

    private void UpdateSaveState()
    {
        if (_isInitializing || SaveButton is null)
        {
            return;
        }

        var hasPendingChanges = HasContentChanged() || HasReminderChanged();
        SaveButton.IsVisible = hasPendingChanges;
        SaveButton.IsEnabled = hasPendingChanges;
    }

    private void OnReminderCancelClicked(object? sender, EventArgs e)
    {
        ReminderSelectionOverlay.IsVisible = false;
    }

    private async void OnReminderApplyClicked(object? sender, EventArgs e)
    {
        var triggerTime = await BuildTriggerTime();
        if (triggerTime is null)
        {
            return;
        }

        _hasReminder = true;
        if (_workingList is not null)
        {
            _workingList.ReminderAt = triggerTime.Value;
        }

        ReminderSelectionOverlay.IsVisible = false;
        UpdateReminderDisplay();
        UpdateSaveState();
    }

    private async Task SaveStandaloneListAsync(ShoppingList saved)
    {
        var existingLists = (await _shoppingListRepository.LoadAsync()).ToList();
        var existingIndex = existingLists.FindIndex(list => list.Id == saved.Id);

        if (existingIndex >= 0)
        {
            existingLists[existingIndex] = saved.Clone();
        }
        else
        {
            existingLists.Add(saved.Clone());
        }

        await _shoppingListRepository.SaveAsync(existingLists);
    }
}
