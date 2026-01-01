using ChoreNotifier.Common;
using ChoreNotifier.Data;
using ChoreNotifier.Models;
using Microsoft.EntityFrameworkCore;

namespace ChoreNotifier.Features.Chores.CreateChore;

public sealed class CreateChoreHandler
{
    private readonly ChoreDbContext _db;
    public CreateChoreHandler(ChoreDbContext db) => _db = db;

    public async Task<CreateChoreResponse> Handle(CreateChoreRequest req, CancellationToken ct)
    {
        var users = await _db.Users.Where(u => req.AssigneeUserIds.Contains(u.Id)).ToListAsync(ct);
        
        var missingUserIds = req.AssigneeUserIds.Except(users.Select(u => u.Id)).ToList();
        if (missingUserIds.Any())
            throw new NotFoundException("Users", string.Join(", ", missingUserIds));
        
        var chore = new Chore
        {
            Title = req.Title,
            Description = req.Description,
            ChoreSchedule = req.ChoreSchedule.ToDomain(),
            AllowSnooze = req.AllowSnooze,
            SnoozeDuration = req.SnoozeDuration,
        };
        _db.Chores.Add(chore);
        foreach (var user in users)
        {
            chore.AddAssignee(user);
        }
        await _db.SaveChangesAsync(ct);

        return chore.ToResponse();
    }
}

public static class ChoreMappings
{
    public static CreateChoreResponse ToResponse(this Chore chore)
    {
        return new CreateChoreResponse
        {
            Id = chore.Id,
            Title = chore.Title,
            Description = chore.Description,
            ChoreSchedule = chore.ChoreSchedule.ToResponse(),
            AllowSnooze = chore.AllowSnooze,
            SnoozeDuration = chore.SnoozeDuration,
            Assignees = chore.Assignees
                .OrderBy(a => a.Order)
                .Select(a => new ChoreAssigneeResponse
                {
                    Id = a.User.Id,
                    Name = a.User.Name
                })
                .ToList()
        };
    }
}

public static class ChoreScheduleMappings
{
    public static ChoreSchedule ToDomain(this CreateChoreScheduleRequest request)
    {
        return request switch
        {
            WeekdayAndTimeCreateChoreScheduleRequest weekdaySchedule => new WeekdayAndTimeChoreSchedule
            {
                Start = weekdaySchedule.Start,
                Until = weekdaySchedule.Until,
                Weekday = weekdaySchedule.Weekday,
                Time = weekdaySchedule.Time
            },
            _ => throw new NotSupportedException($"Schedule type {request.GetType().Name} is not supported")
        };
    }

    public static ChoreScheduleResponse ToResponse(this ChoreSchedule schedule)
    {
        return schedule switch
        {
            WeekdayAndTimeChoreSchedule weekday => new WeekdayAndTimeChoreScheduleResponse
            {
                Start = weekday.Start,
                Until = weekday.Until,
                Weekday = weekday.Weekday,
                Time = weekday.Time
            },
            _ => throw new NotSupportedException($"Schedule type {schedule.GetType().Name} is not supported")
        };
    }
}
