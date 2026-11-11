using UnoCompletionAnimations.Components.Completion;
namespace UnoCompletionAnimations.Components.Completion.Cascade;

public sealed class CascadeCompletionOptions
{
    public TimeSpan StepDuration { get; init; } = CompletionMotionTokens.CascadeStepDuration;
    public TimeSpan SiblingShiftDuration { get; init; } = CompletionMotionTokens.SiblingShiftDuration;
    public TimeSpan MinimumTravelDuration { get; init; } = CompletionMotionTokens.MinimumTravelDuration;
    public TimeSpan MaximumTravelDuration { get; init; } = CompletionMotionTokens.MaximumTravelDuration;
    public double CascadeTimelinePortion { get; init; } = 0.74;
    public double DockTimelineStart { get; init; } = 0.79;
    public bool? ReduceMotionOverride { get; init; }
}