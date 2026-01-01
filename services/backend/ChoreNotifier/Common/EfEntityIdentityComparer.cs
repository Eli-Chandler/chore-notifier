namespace ChoreNotifier.Common;

public sealed class EfEntityIdentityComparer<T> : IEqualityComparer<T>
    where T : class
{
    private readonly Func<T, int> _getId;

    public EfEntityIdentityComparer(Func<T, int> getId) => _getId = getId;

    public bool Equals(T? x, T? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        var xId = _getId(x);
        var yId = _getId(y);

        // If both are persisted, compare by key
        if (xId != 0 && yId != 0) return xId == yId;

        return false;
    }

    public int GetHashCode(T obj)
    {
        var id = _getId(obj);

        return id != 0 ? id.GetHashCode() : obj.GetHashCode();
    }
}
