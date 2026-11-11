using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UnoCompletionAnimations.Samples;

namespace UnoCompletionAnimations;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        InitializeComponent();
        Loaded += (_, _) => NavigateDust();
    }

    private void Dust_Click(object sender, RoutedEventArgs e) => NavigateDust();
    private void Cascade_Click(object sender, RoutedEventArgs e) => ContentFrame.Navigate(typeof(CascadingCompletionSamplePage));
    private void NavigateDust() => ContentFrame.Navigate(typeof(DustCompletionSamplePage));
}