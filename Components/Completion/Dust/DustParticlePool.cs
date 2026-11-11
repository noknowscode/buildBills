using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace UnoCompletionAnimations.Components.Completion.Dust;

internal sealed class DustParticlePool
{
    private readonly Stack<FrameworkElement> _circles = new();
    private readonly Stack<FrameworkElement> _shards = new();

    public FrameworkElement Acquire(bool shard, Brush fill)
    {
        var pool = shard ? _shards : _circles;
        if (pool.TryPop(out var element))
        {
            if (element is Shape shape) shape.Fill = fill;
            else if (element is Border border) border.Background = fill;
            element.Opacity = 1;
            element.RenderTransform = new CompositeTransform();
            return element;
        }

        FrameworkElement created = shard
            ? new Border { Width = 10, Height = 2, CornerRadius = new CornerRadius(999), Background = fill }
            : new Ellipse { Width = 6, Height = 6, Fill = fill };
        created.IsHitTestVisible = false;
        created.RenderTransform = new CompositeTransform();
        created.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
        return created;
    }

    public void Release(FrameworkElement element, bool shard)
    {
        element.Opacity = 0;
        element.RenderTransform = new CompositeTransform();
        (shard ? _shards : _circles).Push(element);
    }

    public void Clear()
    {
        _circles.Clear();
        _shards.Clear();
    }
}