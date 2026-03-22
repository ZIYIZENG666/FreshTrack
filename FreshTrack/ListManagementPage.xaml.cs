using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;

namespace FreshTrack;

public partial class ListManagementPage : ContentPage
{
    public ObservableCollection<ShoppingList> Lists { get; } = new();

    // Bindable property to control delete mode from XAML bindings
    public static readonly BindableProperty IsDeleteModeProperty = BindableProperty.Create(
        nameof(IsDeleteMode), typeof(bool), typeof(ListManagementPage), false);

    public bool IsDeleteMode
    {
        get => (bool)GetValue(IsDeleteModeProperty);
        set => SetValue(IsDeleteModeProperty, value);
    }

    // Tracks selected items when in delete mode
    private IList<object> _selectedItems = new List<object>();
    private bool _isDeleteMode = false;

    public ListManagementPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    private async void OnAddListClicked(object? sender, EventArgs e)
    {
        // Navigate to detail page to create a new list. Provide a callback to add the list when saved.
        var newList = new ShoppingList();
        await Navigation.PushAsync(new GroceryListDetailPage(newList, saved =>
        {
            if (saved != null)
            {
                Lists.Add(saved);
            }
        }));
    }

    private async void OnListClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is ShoppingList list)
        {
            // navigate to detail page for editing; on save replace the item to refresh UI
            await Navigation.PushAsync(new GroceryListDetailPage(list, saved =>
            {
                if (saved == null) return;
                var idx = Lists.IndexOf(list);
                if (idx >= 0)
                {
                    Lists[idx] = saved;
                }
            }));
        }
    }

    private void OnEnterDeleteModeClicked(object? sender, EventArgs e)
    {
        // Enter delete mode: show X buttons on each item
        IsDeleteMode = true;
        _isDeleteMode = true;
        // hide primary buttons and show cancel
        AddListButton.IsVisible = false;
        DeleteListButton.IsVisible = false;
        CancelDeleteButton.IsVisible = true;
        // hide selection-based delete panel if present
        DeletePanel.IsVisible = false;
        // disable selection
        ListcollectionView.SelectionMode = SelectionMode.None;
    }

    private void OnCancelDeleteModeClicked(object? sender, EventArgs e)
    {
        ExitDeleteMode();
    }

    private async void OnDeleteItemClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is ShoppingList list)
        {
            var ok = await DisplayAlertAsync("Delete List", $"Are you sure you want to delete '{list.Name}'?", "Delete", "Cancel");
            if (ok)
            {
                Lists.Remove(list);
            }
        }
    }

    private void OnCancelDeleteClicked(object? sender, EventArgs e)
    {
        ExitDeleteMode();
    }

    private async void OnConfirmDeleteClicked(object? sender, EventArgs e)
    {
        if (_selectedItems.Count == 0)
        {
            await DisplayAlertAsync("Notice", "Please select at least one list to delete.", "OK");
            return;
        }

        var ok = await DisplayAlertAsync("Confirm Delete", $"Are you sure you want to delete the selected {_selectedItems.Count} lists?", "Delete", "Cancel");
        if (!ok) return;

        // Remove selected items
        var toRemove = _selectedItems.Cast<ShoppingList>().ToList();
        foreach (var item in toRemove)
        {
            Lists.Remove(item);
        }

        ExitDeleteMode();
    }

    private void ExitDeleteMode()
    {
        _isDeleteMode = false;
        IsDeleteMode = false;
        ListcollectionView.SelectionMode = SelectionMode.None;
        DeletePanel.IsVisible = false;
        AddListButton.IsVisible = true;
        DeleteListButton.IsVisible = true;
        CancelDeleteButton.IsVisible = false;
        _selectedItems.Clear();
        ListcollectionView.SelectedItems?.Clear();
    }

    private async void OnEditListSwiped(object? sender, EventArgs e)
    {
        if (sender is SwipeItem si && si.CommandParameter is ShoppingList list)
        {
            var newName = await DisplayPromptAsync("Edit List", "Edit name:", "OK", "Cancel", list.Name, -1, Keyboard.Text);
            if (!string.IsNullOrWhiteSpace(newName))
            {
                list.Name = newName.Trim();
                // Refresh the CollectionView
                var idx = Lists.IndexOf(list);
                if (idx >= 0)
                {
                    Lists[idx] = list;
                }
            }
        }
    }

    private async void OnDeleteListSwiped(object? sender, EventArgs e)
    {
        if (sender is SwipeItem si && si.CommandParameter is ShoppingList list)
        {
            var ok = await DisplayAlertAsync("Delete List", $"Are you sure you want to delete '{list.Name}'?", "Delete", "Cancel");
            if (ok)
            {
                Lists.Remove(list);
            }
        }
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!_isDeleteMode) return;

        // Keep track of selected items
        _selectedItems = ListcollectionView.SelectedItems ?? new List<object>();
    }
}

public class ShoppingList
{
    // 新增Id属性，用于区分不同清单的定时提醒
    public int Id { get; set; } = new Random().Next(1000, 9999); // 默认生成随机ID
    public string Name { get; set; } = string.Empty;
    public string Vegetable { get; set; } = string.Empty;
    public string Meat { get; set; } = string.Empty;
}
