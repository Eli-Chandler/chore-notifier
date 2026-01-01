// using System.ComponentModel.DataAnnotations;
// using ChoreNotifier.Common;
// using ChoreNotifier.Data;
// using ChoreNotifier.Features.Chores.CreateChore;
//
// namespace ChoreNotifier.Features.Chores;
//
// public sealed record UpdateChoreRequest
// {
//     [MinLength(1), MaxLength(100)] 
//     public required string Title { get; init; }
//     [MaxLength(1000)]
//     public string? Description { get; init; }
//     public bool AllowSnooze { get; init; } = false;
//     public TimeSpan SnoozeDuration { get; init; } = TimeSpan.FromDays(1);
//     public required CreateChoreScheduleRequest? ChoreSchedule { get; init; }
// }
//
// public sealed record UpdateChoreResponse
// {
//     public required int Id { get; init; }
//     public required string Title { get; init; }
//     public string? Description { get; init; }
//     public required ChoreScheduleResponse ChoreSchedule { get; init; }
//     public required bool AllowSnooze { get; init; }
//     public required TimeSpan SnoozeDuration { get; init; }
// }
//
// public sealed class UpdateChoreHandler
// {
//     private readonly ChoreDbContext _db;
//     public UpdateChoreHandler(ChoreDbContext db) => _db = db;
//
//     public async Task<UpdateChoreResponse> Handle(int choreId, UpdateChoreRequest req, CancellationToken ct)
//     {
//         var chore = await _db.Chores.FindAsync(new object?[] { choreId }, ct);
//         if (chore == null)
//             throw new NotFoundException("Chore", choreId.ToString());
//
//         chore.Title = req.Title;
//         chore.Description = req.Description;
//         chore.AllowSnooze = req.AllowSnooze;
//         chore.SnoozeDuration = req.SnoozeDuration;
//         if (req.ChoreSchedule != null)
//             chore.ChoreSchedule = req.ChoreSchedule.ToDomain();
//
//         await _db.SaveChangesAsync(ct);
//         
//         return new UpdateChoreResponse
//         {
//             Id = chore.Id,
//             Title = chore.Title,
//             Description = chore.Description,
//             AllowSnooze = chore.AllowSnooze,
//             SnoozeDuration = chore.SnoozeDuration,
//             ChoreSchedule = chore.ChoreSchedule.ToResponse()
//         };
//     }
// }