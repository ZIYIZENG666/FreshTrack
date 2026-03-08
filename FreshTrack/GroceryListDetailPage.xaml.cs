using System;
using Microsoft.Maui.Controls;

namespace FreshTrack;

public partial class GroceryListDetailPage : ContentPage
{
    private ShoppingList? _editingList;
    private Action<ShoppingList?>? _onSave;
    private bool _isNew = false;
    private bool _isDirty = false;
    private string? _originalVegetable;
    private string? _originalMeat;
    private string? _originalName;

    public GroceryListDetailPage()
    {
        InitializeComponent();
    }

    public GroceryListDetailPage(ShoppingList list, Action<ShoppingList> onSave) : this()
    {
        _editingList = list;
        _onSave = onSave;
        BindingContext = _editingList;

        // initialize UI
        NameEntry.Text = _editingList.Name;
        VegetableEntry.Text = _editingList.Vegetable;
        MeatEntry.Text = _editingList.Meat;
        _originalVegetable = _editingList.Vegetable;
        _originalMeat = _editingList.Meat;
        _originalName = _editingList.Name;
        _isNew = false;
        SaveButton.IsVisible = false; // only show when content changes
    }

    public GroceryListDetailPage(Action<ShoppingList> onSave) : this()
    {
        _editingList = new ShoppingList();
        _onSave = onSave;
        BindingContext = _editingList;
        _isNew = true;
        SaveButton.IsVisible = true; // new list: show save
        _originalName = string.Empty;
    }

    private async void OnClockClicked(object? sender, EventArgs e)
    {
        await DisplayAlertAsync("Clock", "Clock clicked", "OK");
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        // Use Shell navigation to go back if available
        try
        {
            await Shell.Current.GoToAsync("..");
        }
        catch
        {
            // fallback
            await Navigation.PopAsync();
        }
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        if (_editingList == null) return;
        // collect values from inputs
        var nameInput = NameEntry?.Text?.Trim();
        var veg = VegetableEntry?.Text?.Trim() ?? string.Empty;
        var meat = MeatEntry?.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(veg) && string.IsNullOrWhiteSpace(meat))
        {
            await DisplayAlertAsync("Notice", "Please enter at least one item in Vegetable or Meat.", "OK");
            return;
        }

        _editingList.Vegetable = veg;
        _editingList.Meat = meat;

        // prefer explicit name entry if provided, otherwise ensure display name
        if (!string.IsNullOrWhiteSpace(nameInput))
        {
            _editingList.Name = nameInput!;
        }
        else if (string.IsNullOrWhiteSpace(_editingList.Name))
        {
            _editingList.Name = string.IsNullOrWhiteSpace(veg) ? meat : (string.IsNullOrWhiteSpace(meat) ? veg : $"{veg} & {meat}");
        }
        _onSave?.Invoke(_editingList);
        await Navigation.PopAsync();
    }

    private void OnContentChanged(object? sender, TextChangedEventArgs e)
    {
        // mark dirty if content differs from original
        var name = NameEntry?.Text ?? string.Empty;
        var veg = VegetableEntry?.Text ?? string.Empty;
        var meat = MeatEntry?.Text ?? string.Empty;

        _isDirty = _isNew || name != (_originalName ?? string.Empty) || veg != (_originalVegetable ?? string.Empty) || meat != (_originalMeat ?? string.Empty);
        SaveButton.IsVisible = _isDirty;
    }
}
