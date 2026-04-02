namespace FreshTrack.Controls;

public class AnimatedButton : Button
{
    private bool _isClickAnimationRunning;

    public AnimatedButton()
    {
        Pressed += OnPressed;
        Released += OnReleased;
        Clicked += OnClicked;
    }

    private async void OnPressed(object? sender, EventArgs e)
    {
        if (!IsEnabled)
        {
            return;
        }

        await this.ScaleToAsync(0.96, 80, Easing.CubicOut);
    }

    private async void OnReleased(object? sender, EventArgs e)
    {
        if (!IsEnabled)
        {
            return;
        }

        await this.ScaleToAsync(1, 90, Easing.CubicOut);
    }

    private async void OnClicked(object? sender, EventArgs e)
    {
        if (!IsEnabled || _isClickAnimationRunning)
        {
            return;
        }

        _isClickAnimationRunning = true;

        try
        {
            await this.ScaleToAsync(0.98, 40, Easing.CubicIn);
            await this.ScaleToAsync(1.03, 80, Easing.CubicOut);
            await this.ScaleToAsync(1, 90, Easing.BounceOut);
        }
        finally
        {
            _isClickAnimationRunning = false;
        }
    }
}
