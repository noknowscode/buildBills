using UnoCompletionAnimations.Components.Completion;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using System.Numerics;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace UnoCompletionAnimations.Components.Completion.Dust;

public sealed partial class DustCompletionControl : UserControl
{
    private readonly DustParticlePool _particlePool = new();
    private CancellationTokenSource? _activeRun;

    public DustCompletionControl()
    {
        InitializeComponent();
        Unloaded += (_, _) => CancelActiveAnimation();
    }

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(DustCompletionControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(
        nameof(Subtitle), typeof(string), typeof(DustCompletionControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty AmountTextProperty = DependencyProperty.Register(
        nameof(AmountText), typeof(string), typeof(DustCompletionControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsCompletedProperty = DependencyProperty.Register(
        nameof(IsCompleted), typeof(bool), typeof(DustCompletionControl), new PropertyMetadata(false));

    public static readonly DependencyProperty IsCompletionEnabledProperty = DependencyProperty.Register(
        nameof(IsCompletionEnabled), typeof(bool), typeof(DustCompletionControl), new PropertyMetadata(true));

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Subtitle { get => (string)GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }
    public string AmountText { get => (string)GetValue(AmountTextProperty); set => SetValue(AmountTextProperty, value); }
    public bool IsCompleted { get => (bool)GetValue(IsCompletedProperty); set => SetValue(IsCompletedProperty, value); }
    public bool IsCompletionEnabled { get => (bool)GetValue(IsCompletionEnabledProperty); set => SetValue(IsCompletionEnabledProperty, value); }

    public DustCompletionOptions Options { get; set; } = new();
    public CompletionAnimationState AnimationState { get; private set; } = CompletionAnimationState.Idle;

    public event EventHandler? CompletionRequested;
    public event EventHandler? AnimationCompleted;

    private async void CompletionButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await PlayCompletionAsync();
        }
        catch (OperationCanceledException)
        {
        }
    }

    public async Task<bool> PlayCompletionAsync(CancellationToken cancellationToken = default)
    {
        if (!IsCompletionEnabled || IsCompleted || AnimationState is not CompletionAnimationState.Idle)
        {
            return false;
        }

        CompletionRequested?.Invoke(this, EventArgs.Empty);
        _activeRun = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _activeRun.Token;
        CompletionButton.IsEnabled = false;

        try
        {
            if (ShouldReduceMotion())
            {
                SetCompletedVisualState();
                if (Options.CollapseOnComplete)
                {
                    Root.Visibility = Visibility.Collapsed;
                }
                AnimationState = CompletionAnimationState.Completed;
                AnimationCompleted?.Invoke(this, EventArgs.Empty);
                return true;
            }

            await RunPressAsync(token);
            await RunConfirmAsync(token);
            await RunDissolveAndParticlesAsync(token);
            await RunSettleAsync(token);
            if (Options.CollapseOnComplete)
            {
                await RunCollapseAsync(token);
            }

            AnimationState = CompletionAnimationState.Completed;
            AnimationCompleted?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch (OperationCanceledException)
        {
            RestoreVisualState();
            AnimationState = CompletionAnimationState.Idle;
            throw;
        }
        finally
        {
            CompletionButton.IsEnabled = IsCompletionEnabled && AnimationState is CompletionAnimationState.Idle;
            _activeRun?.Dispose();
            _activeRun = null;
        }
    }

    public void Reset()
    {
        CancelActiveAnimation();
        Root.Visibility = Visibility.Visible;
        Root.Height = double.NaN;
        Root.Opacity = 1;
        RowSurface.Opacity = 1;
        AmountPresenter.Opacity = 1;
        ButtonTransform.ScaleX = ButtonTransform.ScaleY = 1;
        RingTransform.ScaleX = RingTransform.ScaleY = 1;
        CheckTransform.ScaleX = CheckTransform.ScaleY = 1;
        CompletionRing.Background = new SolidColorBrush(Colors.White);
        CompletionRing.BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 185, 204, 189));
        CompletionCheck.Opacity = 0;
        CompletionCheck.StrokeDashOffset = 20;
        ParticleHost.Children.Clear();
        _particlePool.Clear();
        IsCompleted = false;
        AnimationState = CompletionAnimationState.Idle;
        CompletionButton.IsEnabled = IsCompletionEnabled;
    }

    private async Task RunPressAsync(CancellationToken token)
    {
        AnimationState = CompletionAnimationState.Pressed;
        var storyboard = new Storyboard();
        storyboard.Children.Add(StoryboardExtensions.DoubleTo(CompletionButton, "(UIElement.RenderTransform).(CompositeTransform.ScaleX)", 0.94, CompletionMotionTokens.PressDuration, CompletionMotionTokens.ExitEase));
        storyboard.Children.Add(StoryboardExtensions.DoubleTo(CompletionButton, "(UIElement.RenderTransform).(CompositeTransform.ScaleY)", 0.94, CompletionMotionTokens.PressDuration, CompletionMotionTokens.ExitEase));
        await storyboard.BeginAsync(token);
    }

    private async Task RunConfirmAsync(CancellationToken token)
    {
        AnimationState = CompletionAnimationState.Confirming;
        CompletionRing.Background = new SolidColorBrush(ColorHelper.FromArgb(255, 71, 129, 88));
        CompletionRing.BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 71, 129, 88));
        CompletionCheck.Opacity = 1;

        var storyboard = new Storyboard();
        storyboard.Children.Add(BuildSplineAnimation(CompletionRing, "(UIElement.RenderTransform).(CompositeTransform.ScaleX)", new[] { (0d, .82), (.58, 1.07), (1d, 1d) }, CompletionMotionTokens.RingDuration));
        storyboard.Children.Add(BuildSplineAnimation(CompletionRing, "(UIElement.RenderTransform).(CompositeTransform.ScaleY)", new[] { (0d, .82), (.58, 1.07), (1d, 1d) }, CompletionMotionTokens.RingDuration));
        storyboard.Children.Add(StoryboardExtensions.DoubleTo(CompletionButton, "(UIElement.RenderTransform).(CompositeTransform.ScaleX)", 1, CompletionMotionTokens.RingDuration, CompletionMotionTokens.EnterEase));
        storyboard.Children.Add(StoryboardExtensions.DoubleTo(CompletionButton, "(UIElement.RenderTransform).(CompositeTransform.ScaleY)", 1, CompletionMotionTokens.RingDuration, CompletionMotionTokens.EnterEase));
        storyboard.Children.Add(StoryboardExtensions.DoubleTo(CompletionCheck, nameof(Path.StrokeDashOffset), 0, CompletionMotionTokens.CheckDuration, CompletionMotionTokens.EnterEase, TimeSpan.FromMilliseconds(90)));
        await storyboard.BeginAsync(token);
    }

    private async Task RunDissolveAndParticlesAsync(CancellationToken token)
    {
        AnimationState = CompletionAnimationState.Dissolving;

        var amountOrigin = AmountPresenter.TransformToVisual(ParticleHost).TransformPoint(new Point(0, 0));
        var checkOrigin = CompletionButton.TransformToVisual(ParticleHost).TransformPoint(new Point(CompletionButton.ActualWidth / 2, CompletionButton.ActualHeight / 2));
        var count = Math.Clamp(Options.ParticleCount, 8, 32);
        var random = new Random(Options.RandomSeed);

        var amountFade = new Storyboard();
        amountFade.Children.Add(StoryboardExtensions.DoubleTo(AmountPresenter, nameof(Opacity), 0, CompletionMotionTokens.DissolveDuration, CompletionMotionTokens.StandardEase));
        _ = amountFade.BeginAsync(token);

        AnimationState = CompletionAnimationState.Absorbing;
        var particleStoryboard = new Storyboard();
        var created = new List<(FrameworkElement Element, bool Shard)>();
        var palette = new[]
        {
            ColorHelper.FromArgb(255, 95, 140, 105),
            ColorHelper.FromArgb(255, 127, 164, 136),
            ColorHelper.FromArgb(255, 166, 192, 171)
        };

        for (var i = 0; i < count; i++)
        {
            var shard = i % 4 == 0;
            var fill = new SolidColorBrush(palette[i % palette.Length]);
            var element = _particlePool.Acquire(shard, fill);
            created.Add((element, shard));
            ParticleHost.Children.Add(element);

            var x = amountOrigin.X + random.NextDouble() * Math.Max(1, AmountPresenter.ActualWidth) - (shard ? 5 : 3);
            var y = amountOrigin.Y + random.NextDouble() * Math.Max(1, AmountPresenter.ActualHeight) - (shard ? 1 : 3);
            Canvas.SetLeft(element, x);
            Canvas.SetTop(element, y);

            var scatterX = (random.NextDouble() * 2 - 1) * Options.ScatterRadius;
            var scatterY = (random.NextDouble() * 2 - 1) * 9;
            var endX = checkOrigin.X - x + (random.NextDouble() * 8 - 4);
            var endY = checkOrigin.Y - y + (random.NextDouble() * 8 - 4);
            var delay = TimeSpan.FromMilliseconds(i * 10);
            var duration = CompletionMotionTokens.ParticleDuration + TimeSpan.FromMilliseconds(random.NextDouble() * 110);

            particleStoryboard.Children.Add(BuildParticleAnimation(element, "(UIElement.RenderTransform).(CompositeTransform.TranslateX)", scatterX, endX, duration, delay));
            particleStoryboard.Children.Add(BuildParticleAnimation(element, "(UIElement.RenderTransform).(CompositeTransform.TranslateY)", scatterY, endY, duration, delay));
            particleStoryboard.Children.Add(BuildScaleAnimation(element, "(UIElement.RenderTransform).(CompositeTransform.ScaleX)", duration, delay));
            particleStoryboard.Children.Add(BuildScaleAnimation(element, "(UIElement.RenderTransform).(CompositeTransform.ScaleY)", duration, delay));
            particleStoryboard.Children.Add(BuildOpacityAnimation(element, duration, delay));
            particleStoryboard.Children.Add(StoryboardExtensions.DoubleTo(element, "(UIElement.RenderTransform).(CompositeTransform.Rotation)", random.Next(-90, 91), duration, CompletionMotionTokens.StandardEase, delay));
        }

        await particleStoryboard.BeginAsync(token);

        foreach (var (element, shard) in created)
        {
            ParticleHost.Children.Remove(element);
            _particlePool.Release(element, shard);
        }
    }

    private async Task RunSettleAsync(CancellationToken token)
    {
        AnimationState = CompletionAnimationState.Settling;
        var storyboard = new Storyboard();
        storyboard.Children.Add(BuildSplineAnimation(CompletionCheck, "(UIElement.RenderTransform).(CompositeTransform.ScaleX)", new[] { (0d, 1d), (.45, 1.12), (1d, 1d) }, CompletionMotionTokens.PulseDuration));
        storyboard.Children.Add(BuildSplineAnimation(CompletionCheck, "(UIElement.RenderTransform).(CompositeTransform.ScaleY)", new[] { (0d, 1d), (.45, 1.12), (1d, 1d) }, CompletionMotionTokens.PulseDuration));
        await storyboard.BeginAsync(token);
        await Task.Delay(90, token);
    }

    private async Task RunCollapseAsync(CancellationToken token)
    {
        AnimationState = CompletionAnimationState.Collapsing;
        var startHeight = Root.ActualHeight;
        Root.Height = startHeight;
        var storyboard = new Storyboard();
        storyboard.Children.Add(StoryboardExtensions.DoubleTo(Root, nameof(Height), 0, CompletionMotionTokens.CollapseDuration, CompletionMotionTokens.StandardEase));
        storyboard.Children.Add(StoryboardExtensions.DoubleTo(Root, nameof(Opacity), 0, CompletionMotionTokens.CollapseDuration, CompletionMotionTokens.ExitEase));
        await storyboard.BeginAsync(token);
        Root.Visibility = Visibility.Collapsed;
    }

    private static DoubleAnimationUsingKeyFrames BuildSplineAnimation(DependencyObject target, string propertyPath, IEnumerable<(double Offset, double Value)> points, TimeSpan duration)
    {
        var animation = new DoubleAnimationUsingKeyFrames();
        foreach (var (offset, value) in points)
        {
            animation.KeyFrames.Add(new SplineDoubleKeyFrame
            {
                Value = value,
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(duration.TotalMilliseconds * offset)),
                KeySpline = CompletionMotionTokens.SettleSpline
            });
        }
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, propertyPath);
        return animation;
    }

    private static DoubleAnimationUsingKeyFrames BuildParticleAnimation(DependencyObject target, string propertyPath, double scatter, double end, TimeSpan duration, TimeSpan delay)
    {
        var animation = new DoubleAnimationUsingKeyFrames { BeginTime = delay };
        animation.KeyFrames.Add(new SplineDoubleKeyFrame { Value = 0, KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), KeySpline = CompletionMotionTokens.EnterSpline });
        animation.KeyFrames.Add(new SplineDoubleKeyFrame { Value = scatter, KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(duration.TotalMilliseconds * .18)), KeySpline = CompletionMotionTokens.EnterSpline });
        animation.KeyFrames.Add(new SplineDoubleKeyFrame { Value = end * .72, KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(duration.TotalMilliseconds * .72)), KeySpline = CompletionMotionTokens.StandardSpline });
        animation.KeyFrames.Add(new SplineDoubleKeyFrame { Value = end, KeyTime = KeyTime.FromTimeSpan(duration), KeySpline = CompletionMotionTokens.ExitSpline });
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, propertyPath);
        return animation;
    }

    private static DoubleAnimationUsingKeyFrames BuildScaleAnimation(DependencyObject target, string propertyPath, TimeSpan duration, TimeSpan delay)
    {
        var animation = new DoubleAnimationUsingKeyFrames { BeginTime = delay };
        animation.KeyFrames.Add(new SplineDoubleKeyFrame { Value = 1, KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), KeySpline = CompletionMotionTokens.EnterSpline });
        animation.KeyFrames.Add(new SplineDoubleKeyFrame { Value = 1.08, KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(duration.TotalMilliseconds * .18)), KeySpline = CompletionMotionTokens.EnterSpline });
        animation.KeyFrames.Add(new SplineDoubleKeyFrame { Value = .72, KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(duration.TotalMilliseconds * .72)), KeySpline = CompletionMotionTokens.StandardSpline });
        animation.KeyFrames.Add(new SplineDoubleKeyFrame { Value = .22, KeyTime = KeyTime.FromTimeSpan(duration), KeySpline = CompletionMotionTokens.ExitSpline });
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, propertyPath);
        return animation;
    }

    private static DoubleAnimationUsingKeyFrames BuildOpacityAnimation(DependencyObject target, TimeSpan duration, TimeSpan delay)
    {
        var animation = new DoubleAnimationUsingKeyFrames { BeginTime = delay };
        animation.KeyFrames.Add(new DiscreteDoubleKeyFrame { Value = .94, KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero) });
        animation.KeyFrames.Add(new SplineDoubleKeyFrame { Value = 1, KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(duration.TotalMilliseconds * .18)), KeySpline = CompletionMotionTokens.EnterSpline });
        animation.KeyFrames.Add(new SplineDoubleKeyFrame { Value = .7, KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(duration.TotalMilliseconds * .72)), KeySpline = CompletionMotionTokens.StandardSpline });
        animation.KeyFrames.Add(new SplineDoubleKeyFrame { Value = 0, KeyTime = KeyTime.FromTimeSpan(duration), KeySpline = CompletionMotionTokens.ExitSpline });
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, "Opacity");
        return animation;
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

    private void SetCompletedVisualState()
    {
        CompletionRing.Background = new SolidColorBrush(ColorHelper.FromArgb(255, 71, 129, 88));
        CompletionRing.BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 71, 129, 88));
        CompletionCheck.Opacity = 1;
        CompletionCheck.StrokeDashOffset = 0;
        AmountPresenter.Opacity = 0;
    }

    private void RestoreVisualState()
    {
        Root.Visibility = Visibility.Visible;
        Root.Height = double.NaN;
        Root.Opacity = 1;
        AmountPresenter.Opacity = 1;
        ButtonTransform.ScaleX = ButtonTransform.ScaleY = 1;
        RingTransform.ScaleX = RingTransform.ScaleY = 1;
        CheckTransform.ScaleX = CheckTransform.ScaleY = 1;
        CompletionRing.Background = new SolidColorBrush(Colors.White);
        CompletionRing.BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 185, 204, 189));
        CompletionCheck.Opacity = 0;
        CompletionCheck.StrokeDashOffset = 20;
        ParticleHost.Children.Clear();
    }

    private void CancelActiveAnimation()
    {
        if (_activeRun is { IsCancellationRequested: false })
        {
            _activeRun.Cancel();
        }
    }
}