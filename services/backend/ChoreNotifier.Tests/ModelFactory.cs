using ChoreNotifier.Common;
using ChoreNotifier.Data;
using ChoreNotifier.Models;

namespace ChoreNotifier.Tests;

public class ModelFactory
{
    private readonly DatabaseFixture _dbFixture;
    private readonly IClock _clock;
    private int _userCounter = 1;
    private int _choreCounter = 1;

    public ModelFactory(DatabaseFixture dbFixture, IClock clock)
    {
        _dbFixture = dbFixture;
        _clock = clock;
    }

    public async Task<User> CreateUserAsync(
        string? name = null,
        ChoreDbContext? context = null)
    {
        if (context is null)
        {
            await using var ctx = _dbFixture.CreateDbContext();
            return await CreateUserAsync(name, ctx);
        }

        var createUserResult = User.Create(name ?? $"Test User {_userCounter++}");

        if (createUserResult.IsFailed)
            throw new InvalidOperationException("Failed to create User for test user.");

        var user = createUserResult.Value;

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user;
    }

    public async Task<List<User>> CreateUsersAsync(
        int count,
        Func<int, string>? nameFactory = null,
        ChoreDbContext? context = null)
    {
        if (context is null)
        {
            await using var ctx = _dbFixture.CreateDbContext();
            return await CreateUsersAsync(count, nameFactory, ctx);
        }

        var users = new List<User>(count);

        for (int i = 0; i < count; i++)
        {
            var name = nameFactory?.Invoke(i) ?? $"Test User {_userCounter++}";
            var user = await CreateUserAsync(name, context);
            users.Add(user);
        }

        return users;
    }

    public async Task<Chore> CreateChoreAsync(
        string? title = null,
        int numAssignees = 0,
        ChoreDbContext? context = null)
    {
        if (context is null)
        {
            await using var ctx = _dbFixture.CreateDbContext();
            return await CreateChoreAsync(title, numAssignees, ctx);
        }

        var createChoreScheduleResult = ChoreSchedule.Create(
            start: _clock.UtcNow,
            intervalDays: 7
        );

        if (createChoreScheduleResult.IsFailed)
            throw new InvalidOperationException("Failed to create ChoreSchedule for test chore.");

        var createChoreResult = Chore.Create(
            title: title ?? $"Test Chore {_choreCounter++}",
            description: "This is a test chore",
            choreSchedule: createChoreScheduleResult.Value,
            snoozeDuration: TimeSpan.FromDays(2)
        );

        if (createChoreResult.IsFailed)
            throw new InvalidOperationException("Failed to create Chore for test chore.");

        var chore = createChoreResult.Value;

        if (numAssignees > 0)
        {
            var users = await CreateUsersAsync(numAssignees, context: context);
            foreach (var user in users)
            {
                chore.AddAssignee(user);
            }
        }

        context.Chores.Add(chore);
        await context.SaveChangesAsync();

        return chore;
    }

    public async Task<List<Chore>> CreateChoresAsync(
        int count,
        Func<int, string>? titleFactory = null,
        int numAssignees = 0,
        ChoreDbContext? context = null)
    {
        if (context is null)
        {
            await using var ctx = _dbFixture.CreateDbContext();
            return await CreateChoresAsync(count, titleFactory, numAssignees, ctx);
        }

        var chores = new List<Chore>(count);

        for (int i = 0; i < count; i++)
        {
            var title = titleFactory?.Invoke(i) ?? $"Test Chore {_choreCounter++}";
            var chore = await CreateChoreAsync(title, numAssignees, context);
            chores.Add(chore);
        }

        return chores;
    }

    public async Task<ChoreOccurrence> CreateChoreOccurrenceAsync(
        Chore? chore = null,
        User? user = null,
        DateTimeOffset? scheduledAt = null,
        ChoreDbContext? context = null
    )
    {
        if (context is null)
        {
            await using var ctx = _dbFixture.CreateDbContext();
            return await CreateChoreOccurrenceAsync(chore, user, scheduledAt, ctx);
        }

        chore ??= await CreateChoreAsync(numAssignees: 1, context: context);
        scheduledAt ??= _clock.UtcNow;
        user ??= chore.Assignees.First().User;
        chore.AddAssignee(user);

        var choreOccurence = new ChoreOccurrence(chore, user, scheduledAt.Value);
        context.ChoreOccurrences.Add(choreOccurence);
        await context.SaveChangesAsync();
        return choreOccurence;
    }
}
