using System.Collections.ObjectModel;
using UnoCompletionAnimations.Models;

namespace UnoCompletionAnimations.Samples;

public sealed class SamplePaymentsViewModel
{
    public ObservableCollection<CompletionItem> Upcoming { get; } = new();
    public ObservableCollection<CompletionItem> Paid { get; } = new();

    public SamplePaymentsViewModel() => Reset();

    public void Reset()
    {
        Upcoming.Clear();
        Paid.Clear();
        foreach (var item in CreateSeed()) Upcoming.Add(item);
    }

    public Task CommitAsync(CompletionItem item)
    {
        if (Upcoming.Remove(item))
        {
            item.IsCompleted = true;
            Paid.Add(item);
        }
        return Task.CompletedTask;
    }

    private static IEnumerable<CompletionItem> CreateSeed()
    {
        yield return Item("school", "School tax", "Due Nov 2", "$199.71");
        yield return Item("bell", "Bell", "Due Nov 4", "$125.00");
        yield return Item("rogers", "Rogers", "Due Nov 4", "≈ $100.00");
        yield return Item("mortgage", "Mortgage", "Due Nov 6", "$954.00");
        yield return Item("daycare", "Daycare", "Due Nov 15", "$550.00");
        yield return Item("hydro", "Hydro-Québec", "Due Nov 13", "$250.00");
        yield return Item("volkswagen", "Volkswagen", "Due Nov 25", "$515.00");
    }

    private static CompletionItem Item(string id, string title, string subtitle, string amount) => new()
    {
        Id = id,
        Title = title,
        Subtitle = subtitle,
        AmountText = amount
    };
}