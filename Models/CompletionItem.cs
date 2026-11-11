using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UnoCompletionAnimations.Models;

public sealed class CompletionItem : INotifyPropertyChanged
{
    private bool _isCompleted;

    public required string Id { get; init; }
    public required string Title { get; init; }
    public string Subtitle { get; init; } = string.Empty;
    public required string AmountText { get; init; }

    public bool IsCompleted
    {
        get => _isCompleted;
        set
        {
            if (_isCompleted == value) return;
            _isCompleted = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCompleted)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}