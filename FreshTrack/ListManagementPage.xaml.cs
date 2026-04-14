using System.Collections.ObjectModel;

namespace FreshTrack;

public partial class ListManagementPage : ContentPage
{
    public static readonly BindableProperty IsDeleteModeProperty = BindableProperty.Create(
        nameof(IsDeleteMode),
        typeof(bool),
        typeof(ListManagementPage),
        false);

    private readonly IShoppingListRepository _shoppingListRepository;
    private readonly IReminderService? _reminderService;
    private bool _isLoading;

    public ObservableCollection<ShoppingList> Lists { get; } = new();

    public bool IsDeleteMode
    {
        get => (bool)GetValue(IsDeleteModeProperty);
        set => SetValue(IsDeleteModeProperty, value);
    }

    public ListManagementPage() : this(ServiceHelper.GetRequiredService<IShoppingListRepository>())
    {
    }

    public ListManagementPage(IShoppingListRepository shoppingListRepository)
    {
        InitializeComponent();
        _shoppingListRepository = shoppingListRepository;
        _reminderService = ServiceHelper.GetService<IReminderService>();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_isLoading)
        {
            return;
        }

        _isLoading = true;

        try
        {
            await LoadListsAsync();
        }
        catch
        {
            await DisplayAlertAsync("Load Failed", "Unable to load your saved lists right now.", "OK");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async void OnAddListClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new GroceryListDetailPage(AddListAsync));
    }

    private async void OnListTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not ShoppingList list)
        {
            return;
        }

        await OpenListAsync(list);
    }

    private void OnEnterDeleteModeClicked(object? sender, EventArgs e)
    {
        SetDeleteMode(true);
    }

    private void OnCancelDeleteModeClicked(object? sender, EventArgs e)
    {
        SetDeleteMode(false);
    }

    private async void OnDeleteItemClicked(object? sender, EventArgs e)
    {
        if (GetListFromSender(sender) is not ShoppingList list)
        {
            return;
        }

        await DeleteListAsync(list);
    }

    private void SetDeleteMode(bool isEnabled)
    {
        IsDeleteMode = isEnabled;
        AddListButton.IsVisible = !isEnabled;
        DeleteListButton.IsVisible = !isEnabled;
        CancelDeleteButton.IsVisible = isEnabled;
    }

    private async Task LoadListsAsync()
    {
        Lists.Clear();

        var savedLists = await _shoppingListRepository.LoadAsync();
        foreach (var list in savedLists)
        {
            Lists.Add(list);
        }
    }

    private async Task AddListAsync(ShoppingList saved)
    {
        Lists.Add(saved);

        try
        {
            await PersistListsAsync();
        }
        catch
        {
            Lists.Remove(saved);
            throw;
        }
    }

    private async Task UpdateListAsync(ShoppingList original, ShoppingList updated)
    {
        var index = Lists.IndexOf(original);
        if (index < 0)
        {
            return;
        }

        Lists[index] = updated;

        try
        {
            await PersistListsAsync();
        }
        catch
        {
            Lists[index] = original;
            throw;
        }
    }

    private async Task DeleteListAsync(ShoppingList list)
    {
        var shouldDelete = await DisplayAlertAsync(
            "Delete List",
            $"Are you sure you want to delete '{list.Name}'?",
            "Delete",
            "Cancel");

        if (!shouldDelete)
        {
            return;
        }

        var index = Lists.IndexOf(list);
        if (index < 0)
        {
            return;
        }

        Lists.RemoveAt(index);

        try
        {
            await PersistListsAsync();
            _reminderService?.CancelReminder(list.Id);
        }
        catch
        {
            Lists.Insert(index, list);
            await DisplayAlertAsync("Delete Failed", "Unable to delete this list right now.", "OK");
        }
    }

    private Task PersistListsAsync()
    {
        return _shoppingListRepository.SaveAsync(Lists);
    }

    private Task OpenListAsync(ShoppingList list)
    {
        return Navigation.PushAsync(new GroceryListDetailPage(list, updated => UpdateListAsync(list, updated)));
    }

    private static ShoppingList? GetListFromSender(object? sender)
    {
        return sender switch
        {
            Button { CommandParameter: ShoppingList list } => list,
            _ => null
        };
    }
}
