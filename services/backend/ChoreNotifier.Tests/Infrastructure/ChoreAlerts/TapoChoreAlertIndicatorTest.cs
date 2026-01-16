using ChoreNotifier.Infrastructure.ChoreAlerts;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ChoreNotifier.Tests.Infrastructure.ChoreAlerts;

[TestSubject(typeof(TapoChoreAlertIndicator))]
public class TapoChoreAlertIndicatorTest
{
    private readonly TapoChoreAlertIndicator _indicator;

    public TapoChoreAlertIndicatorTest()
    {
        var username = Environment.GetEnvironmentVariable("Tapo__Username");
        var password = Environment.GetEnvironmentVariable("Tapo__Password");
        var ipAddress = Environment.GetEnvironmentVariable("Tapo__IpAddress");

        var options = Options.Create(new TapoChoreAlertIndicatorOptions
        {
            Username = username ?? "",
            Password = password ?? "",
            IpAddress = ipAddress ?? ""
        });
        _indicator = new TapoChoreAlertIndicator(options, NullLogger<TapoChoreAlertIndicator>.Instance);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SetStateAsync_WhenOverdue_TurnsLightRed()
    {
        await _indicator.SetStateAsync(ChoreAlertState.Overdue);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SetStateAsync_WhenOk_TurnsLightGreen()
    {
        await _indicator.SetStateAsync(ChoreAlertState.Ok);
    }
}
