namespace UnoCompletionAnimations.Models;

public sealed class CompletionItemEventArgs : EventArgs
{
    public CompletionItemEventArgs(CompletionItem item) => Item = item;
    public CompletionItem Item { get; }
}