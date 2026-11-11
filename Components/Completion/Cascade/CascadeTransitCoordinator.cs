using UnoCompletionAnimations.Components.Completion;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using UnoCompletionAnimations.Models;
using Windows.Foundation;

namespace UnoCompletionAnimations.Components.Completion.Cascade;

internal sealed class CascadeTransitCoordinator
{
    public async Task<CascadeResult> RunAsync(
        Grid animationSurface,
        Canvas transitOverlay,
        FrameworkElement sourceRow,
        IReadOnlyList<FrameworkElement> followingRows,
        FrameworkElement paidTarget,
        CompletionItem item,
        CascadeCompletionOptions options,
        Func<Task> commitAsync,
        CancellationToken cancellationToken)
    {
        var sourceBounds = GetBounds(sourceRow, animationSurface);
        if (sourceBounds.Width <= 0 || sourceBounds.Height <= 0)
        {
            return CascadeResult.Cancelled;
        }

        var following = followingRows
            .Select((element, index) => new CascadeItemContainer(index, element, GetBounds(element, animationSurface)))
            .ToArray();
        var paidBounds = GetBounds(paidTarget, animationSurface);

        var transit = new TransitRowPresenter
        {
            Title = item.Title,
            Subtitle = "Completed",
            AmountText = item.AmountText,
            Width = sourceBounds.Width,
            Height = sourceBounds.Height,
            RenderTransformOrigin = new Point(.5, .5),
            RenderTransform = new CompositeTransform()
        };

        Canvas.SetLeft(transit, sourceBounds.X);
        Canvas.SetTop(transit, sourceBounds.Y);
        transitOverlay.Children.Add(transit);
        sourceRow.Opacity = 0;

        var oldTransforms = new Dictionary<FrameworkElement, Transform?>();
        foreach (var entry in following)
        {
            oldTransforms[entry.Element] = entry.Element.RenderTransform;
            entry.Element.RenderTransform = new CompositeTransform();
        }

        try
        {
            var duration = TimeSpan.FromMilliseconds(Math.Clamp(
                options.MinimumTravelDuration.TotalMilliseconds + following.Length * options.StepDuration.TotalMilliseconds,
                options.MinimumTravelDuration.TotalMilliseconds,
                options.MaximumTravelDuration.TotalMilliseconds));

            var transitTransform = (CompositeTransform)transit.RenderTransform;
            var travel = new Storyboard();
            var translateY = new DoubleAnimationUsingKeyFrames();
            translateY.KeyFrames.Add(new SplineDoubleKeyFrame
            {
                Value = 0,
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero),
                KeySpline = CompletionMotionTokens.StandardSpline
            });

            for (var i = 0; i < following.Length; i++)
            {
                var delta = following[i].Bounds.Y - sourceBounds.Y;
                var normalized = ((i + 1d) / (following.Length + 1d)) * options.CascadeTimelinePortion;
                translateY.KeyFrames.Add(new SplineDoubleKeyFrame
                {
                    Value = delta,
                    KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(duration.TotalMilliseconds * normalized)),
                    KeySpline = CompletionMotionTokens.StandardSpline
                });

                var siblingShift = StoryboardExtensions.DoubleTo(
                    following[i].Element,
                    "(UIElement.RenderTransform).(CompositeTransform.TranslateY)",
                    -sourceBounds.Height,
                    options.SiblingShiftDuration,
                    CompletionMotionTokens.StandardEase,
                    TimeSpan.FromMilliseconds(i * options.StepDuration.TotalMilliseconds + 42));
                travel.Children.Add(siblingShift);
            }

            var lastTravelDelta = following.Length > 0
                ? following[^1].Bounds.Y - sourceBounds.Y
                : Math.Min(sourceBounds.Height * .6, 48);
            var dockDelta = paidBounds.Y + Math.Min(18, paidBounds.Height / 2) - (sourceBounds.Y + Math.Min(18, sourceBounds.Height / 2));

            translateY.KeyFrames.Add(new SplineDoubleKeyFrame
            {
                Value = lastTravelDelta,
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(duration.TotalMilliseconds * options.DockTimelineStart)),
                KeySpline = CompletionMotionTokens.StandardSpline
            });
            translateY.KeyFrames.Add(new SplineDoubleKeyFrame
            {
                Value = dockDelta,
                KeyTime = KeyTime.FromTimeSpan(duration),
                KeySpline = CompletionMotionTokens.ExitSpline
            });
            Storyboard.SetTarget(translateY, transit);
            Storyboard.SetTargetProperty(translateY, "(UIElement.RenderTransform).(CompositeTransform.TranslateY)");
            travel.Children.Add(translateY);

            travel.Children.Add(BuildTransitScale(transit, "(UIElement.RenderTransform).(CompositeTransform.ScaleX)", duration, options.DockTimelineStart));
            travel.Children.Add(BuildTransitScale(transit, "(UIElement.RenderTransform).(CompositeTransform.ScaleY)", duration, options.DockTimelineStart));
            travel.Children.Add(BuildTransitOpacity(transit, duration, options.DockTimelineStart));

            await travel.BeginAsync(cancellationToken);
            await commitAsync();
            animationSurface.UpdateLayout();
            await PulsePaidTargetAsync(paidTarget, cancellationToken);
            return CascadeResult.Completed;
        }
        catch (OperationCanceledException)
        {
            return CascadeResult.Cancelled;
        }
        finally
        {
            sourceRow.Opacity = 1;
            foreach (var entry in following)
            {
                if (oldTransforms.TryGetValue(entry.Element, out var transform))
                {
                    entry.Element.RenderTransform = transform;
                }
            }
            transitOverlay.Children.Remove(transit);
        }
    }

    private static Rect GetBounds(FrameworkElement element, UIElement relativeTo)
    {
        var origin = element.TransformToVisual(relativeTo).TransformPoint(new Point(0, 0));
        return new Rect(origin.X, origin.Y, element.ActualWidth, element.ActualHeight);
    }

    private static DoubleAnimationUsingKeyFrames BuildTransitScale(DependencyObject target, string propertyPath, TimeSpan duration, double dockStart)
    {
        var animation = new DoubleAnimationUsingKeyFrames();
        animation.KeyFrames.Add(new SplineDoubleKeyFrame { Value = 1.006, KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), KeySpline = CompletionMotionTokens.EnterSpline });
        animation.KeyFrames.Add(new SplineDoubleKeyFrame { Value = 1.004, KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(duration.TotalMilliseconds * dockStart)), KeySpline = CompletionMotionTokens.StandardSpline });
        animation.KeyFrames.Add(new SplineDoubleKeyFrame { Value = .965, KeyTime = KeyTime.FromTimeSpan(duration), KeySpline = CompletionMotionTokens.ExitSpline });
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, propertyPath);
        return animation;
    }

    private static DoubleAnimationUsingKeyFrames BuildTransitOpacity(DependencyObject target, TimeSpan duration, double dockStart)
    {
        var animation = new DoubleAnimationUsingKeyFrames();
        animation.KeyFrames.Add(new DiscreteDoubleKeyFrame { Value = 1, KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero) });
        animation.KeyFrames.Add(new DiscreteDoubleKeyFrame { Value = 1, KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(duration.TotalMilliseconds * dockStart)) });
        animation.KeyFrames.Add(new SplineDoubleKeyFrame { Value = .2, KeyTime = KeyTime.FromTimeSpan(duration), KeySpline = CompletionMotionTokens.ExitSpline });
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, "Opacity");
        return animation;
    }

    private static async Task PulsePaidTargetAsync(FrameworkElement paidTarget, CancellationToken token)
    {
        if (paidTarget.RenderTransform is not CompositeTransform)
        {
            paidTarget.RenderTransform = new CompositeTransform();
        }

        var storyboard = new Storyboard();
        storyboard.Children.Add(BuildPulse(paidTarget, "(UIElement.RenderTransform).(CompositeTransform.ScaleX)"));
        storyboard.Children.Add(BuildPulse(paidTarget, "(UIElement.RenderTransform).(CompositeTransform.ScaleY)"));
        await storyboard.BeginAsync(token);
    }

    private static DoubleAnimationUsingKeyFrames BuildPulse(DependencyObject target, string propertyPath)
    {
        var duration = TimeSpan.FromMilliseconds(650);
        var animation = new DoubleAnimationUsingKeyFrames();
        animation.KeyFrames.Add(new SplineDoubleKeyFrame { Value = 1, KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), KeySpline = CompletionMotionTokens.EnterSpline });
        animation.KeyFrames.Add(new SplineDoubleKeyFrame { Value = 1.012, KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(260)), KeySpline = CompletionMotionTokens.SettleSpline });
        animation.KeyFrames.Add(new SplineDoubleKeyFrame { Value = 1, KeyTime = KeyTime.FromTimeSpan(duration), KeySpline = CompletionMotionTokens.StandardSpline });
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, propertyPath);
        return animation;
    }
}