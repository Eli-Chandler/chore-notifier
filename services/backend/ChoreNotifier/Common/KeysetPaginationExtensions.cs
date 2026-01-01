using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Common;

public class KeysetPage<T>
{
    public List<T> Items { get; set; } = new();
    public bool HasNextPage { get; set; }
    public object? NextCursor { get; set; }
    public int PageSize { get; set; }

    public KeysetPage<TResult> Select<TResult>(Func<T, TResult> selector)
    {
        return new KeysetPage<TResult>
        {
            Items = Items.Select(selector).ToList(),
            HasNextPage = HasNextPage,
            NextCursor = NextCursor,
            PageSize = PageSize
        };
    }
}

public static class KeysetPaginationExtensions
{
    public static async Task<KeysetPage<T>> ToKeysetPageAsync<T, TKey>(
        this IOrderedQueryable<T> query,
        Expression<Func<T, TKey>> keySelector,
        TKey? afterKey = default,
        int pageSize = 20,
        int maxPageSize = 100,
        CancellationToken cancellationToken = default)
        where TKey : struct, IComparable<TKey>
    {
        if (pageSize < 1)
            throw new ArgumentException("Page size must be at least 1", nameof(pageSize));

        if (pageSize > maxPageSize)
            throw new ArgumentException(
                $"Page size ({pageSize}) cannot exceed maximum page size ({maxPageSize}). " +
                $"If you need more than {maxPageSize} items, explicitly set a higher maxPageSize.",
                nameof(pageSize));

        var keySelectorFunc = keySelector.Compile();

        IQueryable<T> filteredQuery = query;
        if (afterKey.HasValue)
        {
            var parameter = keySelector.Parameters[0];
            var comparison = Expression.GreaterThan(
                keySelector.Body,
                Expression.Constant(afterKey.Value, typeof(TKey)));

            var lambda = Expression.Lambda<Func<T, bool>>(comparison, parameter);
            filteredQuery = query.Where(lambda);
        }

        // Fetch pageSize + 1 to determine if there are more results
        var items = await filteredQuery
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasNextPage = items.Count > pageSize;
        if (hasNextPage)
        {
            items = items.Take(pageSize).ToList();
        }

        TKey? nextCursor = default;
        if (hasNextPage && items.Any())
        {
            nextCursor = keySelectorFunc(items.Last());
        }

        return new KeysetPage<T>
        {
            Items = items,
            HasNextPage = hasNextPage,
            NextCursor = nextCursor,
            PageSize = pageSize
        };
    }
}