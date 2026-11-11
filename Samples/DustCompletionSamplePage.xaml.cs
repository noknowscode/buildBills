using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UnoCompletionAnimations.Components.Completion.Dust;

namespace UnoCompletionAnimations.Samples;

public sealed partial class DustCompletionSamplePage : Page
{
    public DustCompletionSamplePage() => InitializeComponent();

    private void Dust_AnimationCompleted(object? sender, EventArgs e)
    {
        if (sender is DustCompletionControl control)
        {
            control.IsCompleted = true;
            LastCompleted.Text = $"Last completed: {control.Title}";
        }
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        Mortgage.Reset();
        Bell.Reset();
        Hydro.Reset();
        LastCompleted.Text = "Nothing completed yet";
    }
}