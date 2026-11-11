using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace UnoCompletionAnimations.Components.Completion.Cascade;

internal sealed record CascadeItemContainer(int Index, FrameworkElement Element, Rect Bounds);