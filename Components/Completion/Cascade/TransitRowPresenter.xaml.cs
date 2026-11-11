using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UnoCompletionAnimations.Components.Completion.Cascade;

public sealed partial class TransitRowPresenter : UserControl
{
    public TransitRowPresenter() => InitializeComponent();

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(TransitRowPresenter), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(TransitRowPresenter), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty AmountTextProperty = DependencyProperty.Register(nameof(AmountText), typeof(string), typeof(TransitRowPresenter), new PropertyMetadata(string.Empty));

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Subtitle { get => (string)GetValue(SubtitleProperty); set => SetValue(SubtitleProperty, value); }
    public string AmountText { get => (string)GetValue(AmountTextProperty); set => SetValue(AmountTextProperty, value); }
}