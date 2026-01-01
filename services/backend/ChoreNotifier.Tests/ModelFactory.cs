using ChoreNotifier.Data;
using ChoreNotifier.Models;

namespace ChoreNotifier.Tests;

public class ModelFactory
{
    private readonly DatabaseFixture _dbFixture;
    private int _userCounter = 1;
    private int _choreCounter = 1;

    public ModelFactory(DatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
    }

    public async Task<User> CreateUserAsync(string? name = null)
    {
        await using var context = _dbFixture.CreateDbContext();

        var user = new User
        {
            Name = name ?? $"Test User {_userCounter++}"
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user;
    }

    public async Task<List<User>> CreateUsersAsync(int count, Func<int, string>? nameFactory = null)
    {
        var users = new List<User>();

        for (int i = 0; i < count; i++)
        {
            var name = nameFactory?.Invoke(i) ?? $"Test User {_userCounter++}";
            var user = await CreateUserAsync(name);
            users.Add(user);
        }

        return users;
    }
}