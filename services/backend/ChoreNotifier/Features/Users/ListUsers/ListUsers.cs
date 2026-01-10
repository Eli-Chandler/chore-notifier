using ChoreNotifier.Data;
using ChoreNotifier.Common;
using ChoreNotifier.Models;
using FluentResults;
using MediatR;

namespace ChoreNotifier.Features.Users.ListUsers;

public sealed record ListUsersRequest(int PageSize, int? AfterId) : IRequest<Result<KeysetPage<ListUserResponseItem, int>>>;

public class ListUserResponseItem
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
}

public sealed class ListUsersHandler : IRequestHandler<ListUsersRequest, Result<KeysetPage<ListUserResponseItem, int>>>
{
    private readonly ChoreDbContext _db;
    public ListUsersHandler(ChoreDbContext db) => _db = db;

    public async Task<Result<KeysetPage<ListUserResponseItem, int>>> Handle(ListUsersRequest req, CancellationToken ct = default)
    {
        var validatePageSizeResult = ValidatePageSize(req.PageSize);
        if (validatePageSizeResult.IsFailed)
            return validatePageSizeResult;

        var result = await _db.Users
            .OrderBy(u => u.Id)
            .Where(u => req.AfterId == null || u.Id > req.AfterId)
            .ToKeysetPageAsync(
                req.PageSize,
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
            return Result.Fail(new ValidationError("Page size must be greater than 0"));
        if (pageSize > 100)
            return Result.Fail(new ValidationError("Page size cannot exceed 100"));
        return Result.Ok();
    }
}
