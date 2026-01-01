using ChoreNotifier.Data;
using ChoreNotifier.Common;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Features.Users;


public sealed class DeleteUserHandler
{
    private readonly ChoreDbContext _db;
    public DeleteUserHandler(ChoreDbContext db) => _db = db;
    
    public async Task Handle(int userId, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            throw new NotFoundException("User", userId);
        _db.Users.Remove(user);
        await _db.SaveChangesAsync(ct);
    }
}