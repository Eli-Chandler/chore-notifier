using ChoreNotifier.Data;
using ChoreNotifier.Common;
using FluentResults;

namespace ChoreNotifier.Features.Users.ListUsers;

public class ListUserResponseItem
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
}

public sealed class ListUsersHandler
{
    private readonly ChoreDbContext _db;
    public ListUsersHandler(ChoreDbContext db) => _db = db;

    public async Task<Result<KeysetPage<ListUserResponseItem, int>>> Handle(int pageSize, int? afterId, CancellationToken ct = default)
    {
        var validatePageSizeResult = ValidatePageSize(pageSize);
        if (validatePageSizeResult.IsFailed)
            return validatePageSizeResult;

        var result = await _db.Users
            .OrderBy(u => u.Id)
            .Where(u => afterId == null || u.Id > afterId)
            .ToKeysetPageAsync(
                pageSize,
                u => u.Id,
                ct
            );

        return result.Select(u => new ListUserResponseItem
        {
            Id = u.Id,
            Name = u.Name
        }
        );
    }

    private static Result ValidatePageSize(int pageSize)
    {
        if (pageSize <= 0)
            return Result.Fail("Page size must be greater than 0");
        if (pageSize > 100)
            return Result.Fail("Page size cannot exceed 100");
        return Result.Ok();
    }
}