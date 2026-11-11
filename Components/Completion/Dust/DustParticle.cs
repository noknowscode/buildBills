using System.Numerics;

namespace UnoCompletionAnimations.Components.Completion.Dust;

internal readonly record struct DustParticle(
    Vector2 Start,
    Vector2 Scatter,
    Vector2 TargetOffset,
    float Scale,
    float Rotation,
    bool IsShard);