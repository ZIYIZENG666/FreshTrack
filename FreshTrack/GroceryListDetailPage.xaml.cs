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
        {
            BindingContext = _editingList;
        };

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
        {
            BindingContext = _editingList;
        };
        _isNew = true;
        SaveButton.IsVisible = true; // new list: show save
        _originalName = string.Empty;
    }

    private async void OnClockClicked(object? sender, EventArgs e)
    {
        // 1. 请求通知权限（Android 13+ 必需）
        var reminderService = Application.Current?.Handler?.MauiContext?.Services.GetService<IReminderService>();
        if (reminderService == null)
        {
            await DisplayAlertAsync("Error", "Reminder service not available", "OK");
            return;
        }

        var hasPermission = await reminderService.RequestNotificationPermissionAsync();
        if (!hasPermission)
        {
            await DisplayAlertAsync("Permission Required", "Please grant notification permission to use reminders", "OK");
            return;
        }

        // 修复1：替换Popup为MAUI原生的DisplayPromptAsync（无需额外依赖）
        // 让用户输入时间（格式：HH:mm）
        var timeInput = await DisplayPromptAsync(
            "Set Reminder Time",
            "Enter time in HH:mm format (e.g., 14:30)",
            placeholder: "HH:mm",
            initialValue: DateTime.Now.ToString("HH:mm"));

        // 用户取消输入
        if (string.IsNullOrWhiteSpace(timeInput)) return;

        // 解析用户输入的时间
        if (!TimeSpan.TryParse(timeInput, out TimeSpan selectedTime))
        {
            await DisplayAlertAsync("Invalid Time", "Please enter a valid time (HH:mm)", "OK");
            return;
        }

        // 计算触发时间（今天的选定时间）
        var currentTime = DateTime.Now;
        var triggerTime = new DateTime(
            currentTime.Year,
            currentTime.Month,
            currentTime.Day,
            // 修复2：TimeSpan不是nullable，直接访问Hours/Minutes/Seconds（解决TimeSpan?报错）
            selectedTime.Hours,
            selectedTime.Minutes,
            selectedTime.Seconds);

        // 如果选定时间已过，设置为明天
        if (triggerTime < DateTime.Now)
        {
            triggerTime = triggerTime.AddDays(1);
        }

        // 4. 设置提醒 - 增加空值保护
        var listName = !string.IsNullOrWhiteSpace(NameEntry?.Text)
            ? NameEntry.Text!.Trim()
            : "Grocery List";
        var reminderTitle = $"Reminder: {listName}";
        var reminderMessage = "It's time to check your grocery list!";
        // ShoppingList已添加Id属性，此处无报错
        var listId = _editingList?.Id ?? Random.Shared.Next(1000, 9999);

        reminderService.SetReminder(triggerTime, reminderTitle, reminderMessage, listId);

        // 提示用户设置成功
        await DisplayAlertAsync("Success", $"Reminder set for {triggerTime:HH:mm}", "OK");
    } // 注意：这里删除了多余的}，修复“成员声明中的标记}无效”错误

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
