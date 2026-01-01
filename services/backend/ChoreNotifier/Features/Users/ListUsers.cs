using ChoreNotifier.Data;
using ChoreNotifier.Common;

namespace ChoreNotifier.Features.Users;

public class ListUserResponseItem
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
}

public sealed class ListUsersHandler
{
    private readonly ChoreDbContext _db;
    public ListUsersHandler(ChoreDbContext db) => _db = db;
    
    public async Task<KeysetPage<ListUserResponseItem>> Handle(int pageSize, int? afterId, CancellationToken ct)
    {
        var result = await _db.Users
            .OrderBy(u => u.Name)
            .ThenBy(u => u.Id)
            .ToKeysetPageAsync(
                u => u.Id,
                afterId,
                pageSize,
                cancellationToken: ct
            );

        return result.Select(u => new ListUserResponseItem()
        {
            Id = u.Id,
            Name = u.Name
        });
    }
}