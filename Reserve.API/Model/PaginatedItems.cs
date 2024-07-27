namespace Reserve.API.Model;

public class PaginatedItems<TEntity>(long count, IEnumerable<TEntity> data) where TEntity : class
{
    public long Count { get; } = count;

    public IEnumerable<TEntity> Data { get;} = data;
}