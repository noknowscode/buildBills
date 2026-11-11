using UnoCompletionAnimations.Components.Completion;
namespace UnoCompletionAnimations.Components.Completion.Dust;

public sealed class DustCompletionOptions
{
    public int ParticleCount { get; init; } = CompletionMotionTokens.DefaultParticleCount;
    public double ScatterRadius { get; init; } = 14;
    public bool CollapseOnComplete { get; init; } = true;
    public bool? ReduceMotionOverride { get; init; }
    public int RandomSeed { get; init; } = 1337;
}