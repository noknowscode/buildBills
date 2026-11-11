using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;

namespace UnoCompletionAnimations.Components.Completion;

internal static class StoryboardExtensions
{
    public static Task BeginAsync(this Storyboard storyboard, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        void Completed(object? sender, object e)
        {
            storyboard.Completed -= Completed;
            tcs.TrySetResult(true);
        }

        storyboard.Completed += Completed;
        var registration = cancellationToken.Register(() =>
        {
            storyboard.Stop();
            storyboard.Completed -= Completed;
            tcs.TrySetCanceled(cancellationToken);
        });
        storyboard.Begin();
        return AwaitAndDisposeAsync();

        async Task AwaitAndDisposeAsync()
        {
            try
            {
                await tcs.Task;
            }
            finally
            {
                registration.Dispose();
            }
        }
    }

    public static DoubleAnimation DoubleTo(
        DependencyObject target,
        string propertyPath,
        double to,
        TimeSpan duration,
        EasingFunctionBase? easing = null,
        TimeSpan? beginTime = null)
    {
        var animation = new DoubleAnimation
        {
            To = to,
            Duration = duration,
            EasingFunction = easing,
            BeginTime = beginTime,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, propertyPath);
        return animation;
    }
}