using UnoCompletionAnimations.Components.Completion;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using UnoCompletionAnimations.Models;
using Windows.UI.ViewManagement;

namespace UnoCompletionAnimations.Components.Completion.Cascade;

public sealed partial class CascadingCompletionList : UserControl
{
    private readonly CascadeTransitCoordinator _coordinator = new();
    private ObservableCollection<CompletionItem>? _upcoming;
    private ObservableCollection<CompletionItem>? _paid;
    private CancellationTokenSource? _activeRun;

    public CascadingCompletionList()
    {
        InitializeComponent();
        Unloaded += (_, _) => CancelActiveAnimation();
    }

    public CascadeCompletionOptions Options { get; set; } = new();
    public CompletionAnimationState AnimationState { get; private set; } = CompletionAnimationState.Idle;
    public bool IsAnimating => AnimationState is not CompletionAnimationState.Idle and not CompletionAnimationState.Completed;

    public Func<CompletionItem, Task>? CommitCompletionAsync { get; set; }

    public event EventHandler<CompletionItemEventArgs>? CompletionRequested;
    public event EventHandler<CompletionItemEventArgs>? AnimationCompleted;

    public void BindCollections(ObservableCollection<CompletionItem> upcoming, ObservableCollection<CompletionItem> paid)
    {
        if (_upcoming is not null) _upcoming.CollectionChanged -= CollectionsChanged;
        if (_paid is not null) _paid.CollectionChanged -= CollectionsChanged;

        _upcoming = upcoming;
        _paid = paid;
        UpcomingRepeater.ItemsSource = _upcoming;
        PaidRepeater.ItemsSource = _paid;
        _upcoming.CollectionChanged += CollectionsChanged;
        _paid.CollectionChanged += CollectionsChanged;
        RefreshCounts();
    }

    private void CollectionsChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshCounts();

    private void RefreshCounts()
    {
        UpcomingCount.Text = _upcoming is null ? "0" : $"{_upcoming.Count} active";
        PaidCount.Text = _paid is null ? "0" : $"{_paid.Count} completed";
    }

    private async void CompleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is CompletionItem item)
        {
            try
            {
                await CompleteItemAsync(item);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    public async Task<bool> CompleteItemAsync(CompletionItem item, CancellationToken cancellationToken = default)
    {
        if (_upcoming is null || _paid is null || IsAnimating || !_upcoming.Contains(item))
        {
            return false;
        }

        CompletionRequested?.Invoke(this, new CompletionItemEventArgs(item));
        _activeRun = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _activeRun.Token;

        try
        {
            if (ShouldReduceMotion())
            {
                await CommitAsync(item);
                AnimationState = CompletionAnimationState.Completed;
                AnimationCompleted?.Invoke(this, new CompletionItemEventArgs(item));
                AnimationState = CompletionAnimationState.Idle;
                return true;
            }

            var sourceIndex = _upcoming.IndexOf(item);
            var sourceRow = UpcomingRepeater.GetOrCreateElement(sourceIndex) as FrameworkElement;
            if (sourceRow is null)
            {
                return false;
            }

            AnimationSurface.UpdateLayout();
            var completionButton = FindDescendant<Button>(sourceRow, "RowCompletionButton");
            var ring = FindDescendant<Border>(sourceRow, "RowCompletionRing");
            var check = FindDescendant<Path>(sourceRow, "RowCompletionCheck");
            if (completionButton is null || ring is null || check is null)
            {
                return false;
            }

            await RunConfirmationAsync(sourceRow, completionButton, ring, check, token);

            AnimationState = CompletionAnimationState.Travelling;
            var following = new List<FrameworkElement>();
            for (var index = sourceIndex + 1; index < _upcoming.Count; index++)
            {
                if (UpcomingRepeater.GetOrCreateElement(index) is FrameworkElement element)
                {
                    following.Add(element);
                }
            }
            AnimationSurface.UpdateLayout();

            var result = await _coordinator.RunAsync(
                AnimationSurface,
                TransitOverlay,
                sourceRow,
                following,
                PaidHeader,
                item,
                Options,
                () => CommitAsync(item),
                token);

            if (result is CascadeResult.Cancelled)
            {
                RestoreRowVisual(sourceRow, completionButton, ring, check);
                AnimationState = CompletionAnimationState.Idle;
                return false;
            }

            AnimationState = CompletionAnimationState.Completed;
            AnimationCompleted?.Invoke(this, new CompletionItemEventArgs(item));
            AnimationState = CompletionAnimationState.Idle;
            return true;
        }
        finally
        {
            _activeRun?.Dispose();
            _activeRun = null;
        }
    }

    private async Task RunConfirmationAsync(
        FrameworkElement row,
        Button completionButton,
        Border ring,
        Path check,
        CancellationToken token)
    {
        AnimationState = CompletionAnimationState.Pressed;
        completionButton.IsEnabled = false;

        if (completionButton.RenderTransform is not CompositeTransform)
        {
            completionButton.RenderTransform = new CompositeTransform();
        }
        if (ring.RenderTransform is not CompositeTransform)
        {
            ring.RenderTransform = new CompositeTransform();
        }

        var press = new Storyboard();
        press.Children.Add(StoryboardExtensions.DoubleTo(completionButton, "(UIElement.RenderTransform).(CompositeTransform.ScaleX)", .94, CompletionMotionTokens.PressDuration, CompletionMotionTokens.ExitEase));
        press.Children.Add(StoryboardExtensions.DoubleTo(completionButton, "(UIElement.RenderTransform).(CompositeTransform.ScaleY)", .94, CompletionMotionTokens.PressDuration, CompletionMotionTokens.ExitEase));
        await press.BeginAsync(token);

        AnimationState = CompletionAnimationState.Confirming;
        ring.Background = new SolidColorBrush(ColorHelper.FromArgb(255, 71, 129, 88));
        ring.BorderBrush = ring.Background;
        check.Opacity = 1;
        if (row is Border border)
        {
            border.Background = new SolidColorBrush(ColorHelper.FromArgb(255, 242, 248, 241));
        }

        var confirm = new Storyboard();
        confirm.Children.Add(BuildRingAnimation(ring, "(UIElement.RenderTransform).(CompositeTransform.ScaleX)"));
        confirm.Children.Add(BuildRingAnimation(ring, "(UIElement.RenderTransform).(CompositeTransform.ScaleY)"));
        confirm.Children.Add(StoryboardExtensions.DoubleTo(completionButton, "(UIElement.RenderTransform).(CompositeTransform.ScaleX)", 1, CompletionMotionTokens.RingDuration, CompletionMotionTokens.EnterEase));
        confirm.Children.Add(StoryboardExtensions.DoubleTo(completionButton, "(UIElement.RenderTransform).(CompositeTransform.ScaleY)", 1, CompletionMotionTokens.RingDuration, CompletionMotionTokens.EnterEase));
        confirm.Children.Add(StoryboardExtensions.DoubleTo(check, nameof(Path.StrokeDashOffset), 0, CompletionMotionTokens.CheckDuration, CompletionMotionTokens.EnterEase, TimeSpan.FromMilliseconds(90)));
        await confirm.BeginAsync(token);
        await Task.Delay(90, token);
    }

    private static DoubleAnimationUsingKeyFrames BuildRingAnimation(DependencyObject target, string propertyPath)
    {
        var animation = new DoubleAnimationUsingKeyFrames();
        animation.KeyFrames.Add(new SplineDoubleKeyFrame { Value = .82, KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), KeySpline = CompletionMotionTokens.EnterSpline });
        animation.KeyFrames.Add(new SplineDoubleKeyFrame { Value = 1.07, KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(170)), KeySpline = CompletionMotionTokens.SettleSpline });
        animation.KeyFrames.Add(new SplineDoubleKeyFrame { Value = 1, KeyTime = KeyTime.FromTimeSpan(CompletionMotionTokens.RingDuration), KeySpline = CompletionMotionTokens.StandardSpline });
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, propertyPath);
        return animation;
    }

    private async Task CommitAsync(CompletionItem item)
    {
        if (CommitCompletionAsync is not null)
        {
            await CommitCompletionAsync(item);
        }
        else if (_upcoming is not null && _paid is not null)
        {
            _upcoming.Remove(item);
            item.IsCompleted = true;
            _paid.Add(item);
        }
    }

    private static void RestoreRowVisual(FrameworkElement row, Button button, Border ring, Path check)
    {
        row.Opacity = 1;
        button.IsEnabled = true;
        if (button.RenderTransform is CompositeTransform buttonTransform)
        {
            buttonTransform.ScaleX = buttonTransform.ScaleY = 1;
        }
        if (ring.RenderTransform is CompositeTransform ringTransform)
        {
            ringTransform.ScaleX = ringTransform.ScaleY = 1;
        }
        ring.Background = new SolidColorBrush(Colors.White);
        ring.BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 185, 204, 189));
        check.Opacity = 0;
        check.StrokeDashOffset = 20;
        if (row is Border border)
        {
            border.Background = (Brush)Application.Current.Resources["SurfaceBrush"];
        }
    }

    private bool ShouldReduceMotion()
    {
        if (Options.ReduceMotionOverride is bool forced)
        {
            return forced;
        }

        try
        {
            return !new UISettings().AnimationsEnabled;
        }
        catch
        {
            return false;
        }
    }

    private static T? FindDescendant<T>(DependencyObject root, string name) where T : FrameworkElement
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < count; index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T typed && typed.Name == name)
            {
                return typed;
            }
            if (FindDescendant<T>(child, name) is { } nested)
            {
                return nested;
            }
        }
        return null;
    }

    private void CancelActiveAnimation()
    {
        if (_activeRun is { IsCancellationRequested: false })
        {
            _activeRun.Cancel();
        }
    }
}