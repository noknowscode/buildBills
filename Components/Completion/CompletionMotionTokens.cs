using Microsoft.UI.Xaml.Media.Animation;

namespace UnoCompletionAnimations.Components.Completion;

public static class CompletionMotionTokens
{
    public static readonly TimeSpan PressDuration = TimeSpan.FromMilliseconds(90);
    public static readonly TimeSpan RingDuration = TimeSpan.FromMilliseconds(300);
    public static readonly TimeSpan CheckDuration = TimeSpan.FromMilliseconds(300);
    public static readonly TimeSpan DissolveDuration = TimeSpan.FromMilliseconds(500);
    public static readonly TimeSpan ParticleDuration = TimeSpan.FromMilliseconds(700);
    public static readonly TimeSpan PulseDuration = TimeSpan.FromMilliseconds(500);
    public static readonly TimeSpan CollapseDuration = TimeSpan.FromMilliseconds(210);
    public static readonly TimeSpan CascadeStepDuration = TimeSpan.FromMilliseconds(145);
    public static readonly TimeSpan SiblingShiftDuration = TimeSpan.FromMilliseconds(190);
    public static readonly TimeSpan MinimumTravelDuration = TimeSpan.FromMilliseconds(520);
    public static readonly TimeSpan MaximumTravelDuration = TimeSpan.FromMilliseconds(1650);

    public const int DefaultParticleCount = 18;

    public static KeySpline StandardSpline => new() { ControlPoint1 = new Windows.Foundation.Point(0.4, 0), ControlPoint2 = new Windows.Foundation.Point(0.2, 1) };
    public static KeySpline EnterSpline => new() { ControlPoint1 = new Windows.Foundation.Point(0, 0), ControlPoint2 = new Windows.Foundation.Point(0.2, 1) };
    public static KeySpline ExitSpline => new() { ControlPoint1 = new Windows.Foundation.Point(0.4, 0), ControlPoint2 = new Windows.Foundation.Point(1, 1) };
    public static KeySpline SettleSpline => new() { ControlPoint1 = new Windows.Foundation.Point(0.22, 1), ControlPoint2 = new Windows.Foundation.Point(0.36, 1) };

    public static CubicEase StandardEase => new() { EasingMode = EasingMode.EaseInOut };
    public static CubicEase EnterEase => new() { EasingMode = EasingMode.EaseOut };
    public static CubicEase ExitEase => new() { EasingMode = EasingMode.EaseIn };
}