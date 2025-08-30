namespace MyFinances.Logic.Interfaces;

public interface IHistoricService
{
    Task GetPostsAsync(int assetId, DateTime dateFrom, DateTime? dateTo);
}
