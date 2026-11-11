using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UnoCompletionAnimations.Models;

namespace UnoCompletionAnimations.Samples;

public sealed partial class CascadingCompletionSamplePage : Page
{
    private readonly SamplePaymentsViewModel _viewModel = new();

    public CascadingCompletionSamplePage()
    {
        InitializeComponent();
        CascadeList.BindCollections(_viewModel.Upcoming, _viewModel.Paid);
        CascadeList.CommitCompletionAsync = _viewModel.CommitAsync;
    }

    private void CascadeList_AnimationCompleted(object? sender, CompletionItemEventArgs e)
        => LastCompleted.Text = $"Last completed: {e.Item.Title}";

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.Reset();
        LastCompleted.Text = "Nothing completed yet";
    }
}