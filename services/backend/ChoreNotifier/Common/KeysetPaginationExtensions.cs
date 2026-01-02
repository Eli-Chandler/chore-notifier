using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Common;

public sealed class KeysetPage<T, TCursor>
    where TCursor : struct
{
    public required List<T> Items { get; init; }
    public required bool HasNextPage { get; init; }
    public required TCursor? NextCursor { get; init; }
    public required int PageSize { get; init; }

    /// <summary>
    /// Canonical builder: expects a list that was fetched with pageSize + 1 items.
    /// Keep this internal/private so callers don't pass the wrong thing.
    /// </summary>
    internal static KeysetPage<T, TCursor> FromOverfetchedResults(
        IReadOnlyList<T> resultsPlusOne,
        int pageSize,
        Func<T, TCursor> cursorSelector)
    {
        if (resultsPlusOne is null) throw new ArgumentNullException(nameof(resultsPlusOne));
        if (cursorSelector is null) throw new ArgumentNullException(nameof(cursorSelector));
        if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));

        var hasNext = resultsPlusOne.Count > pageSize;

        var items = hasNext
            ? resultsPlusOne.Take(pageSize).ToList()
            : resultsPlusOne.ToList();

        var nextCursor = hasNext && items.Count > 0
            ? cursorSelector(items[^1]) : (TCursor?)null;

        return new KeysetPage<T, TCursor>
        {
            Items = items,
            HasNextPage = hasNext,
            NextCursor = nextCursor,
            PageSize = pageSize
        };
    }

    public KeysetPage<TResult, TCursor> Select<TResult>(Func<T, TResult> selector)
    {
        if (selector is null) throw new ArgumentNullException(nameof(selector));

        return new KeysetPage<TResult, TCursor>
        {
            Items = Items.Select(selector).ToList(),
            HasNextPage = HasNextPage,
            NextCursor = NextCursor,
            PageSize = PageSize
        };
    }
}

public static class KeysetPagingExtensions
{
    /// <summary>
    /// Best-practice extension: takes an IQueryable, performs the +1 overfetch internally,
    /// and returns a KeysetPage without callers remembering anything.
    ///
    /// Requires EF Core for ToListAsync. If you're not using EF Core, remove this method.
    /// </summary>
    public static async Task<KeysetPage<T, TCursor>> ToKeysetPageAsync<T, TCursor>(
        this IQueryable<T> query,
        int pageSize,
        Func<T, TCursor> cursorSelector,
        CancellationToken ct = default)
        where TCursor : struct
    {
        if (query is null) throw new ArgumentNullException(nameof(query));
        if (cursorSelector is null) throw new ArgumentNullException(nameof(cursorSelector));
        if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));

        var resultsPlusOne = await query.Take(pageSize + 1).ToListAsync(ct);


        return KeysetPage<T, TCursor>.FromOverfetchedResults(resultsPlusOne, pageSize, cursorSelector);
    }
}