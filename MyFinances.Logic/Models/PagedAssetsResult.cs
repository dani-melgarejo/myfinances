namespace MyFinances.Logic.Models;

public class PagedAssetsResult<T>(IEnumerable<T> items)
{
    public IEnumerable<T> Items { get; set; } = items;

    public int Total { get; set; }
}
