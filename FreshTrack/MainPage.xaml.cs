using System;
using Microsoft.Maui.Controls;

namespace FreshTrack
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnStartTapped(object? sender, TappedEventArgs e)
        {
            await Shell.Current.GoToAsync($"//{AppRoutes.Lists}");
        }
    }
}
