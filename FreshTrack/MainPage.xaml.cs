using System;
using FreshTrack;
using Microsoft.Maui.Controls;

namespace FreshTrack
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnStartClicked(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new ListManagementPage());
        }

        private void OnExitClicked(object? sender, EventArgs e)
        {
            // Force the process to exit on all platforms.
            // Note: On iOS this is discouraged and may lead to App Store rejection.
            System.Environment.Exit(0);
        }
    }
}
