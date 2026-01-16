using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TapoConnect;
using TapoConnect.Dto;

namespace ChoreNotifier.Infrastructure.ChoreAlerts;

public class TapoChoreAlertIndicatorOptions
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string IpAddress { get; set; } = "";

    public bool IsConfigured =>
        !string.IsNullOrEmpty(Username) &&
        !string.IsNullOrEmpty(Password) &&
        !string.IsNullOrEmpty(IpAddress);
}

public class TapoChoreAlertIndicator : IChoreAlertIndicator
{
    private readonly TapoDeviceClient _deviceClient;
    private readonly TapoChoreAlertIndicatorOptions _options;
    private readonly ILogger<TapoChoreAlertIndicator> _logger;

    public TapoChoreAlertIndicator(
        IOptions<TapoChoreAlertIndicatorOptions> options,
        ILogger<TapoChoreAlertIndicator> logger)
    {
        _options = options.Value;
        _logger = logger;
        _deviceClient = new TapoDeviceClient();

        if (!_options.IsConfigured)
            _logger.LogWarning("Tapo chore alert indicator is disabled. Set Tapo:Username, Tapo:Password, and Tapo:IpAddress to enable");
    }

    public async Task SetStateAsync(ChoreAlertState state, CancellationToken ct = default)
    {
        if (!_options.IsConfigured)
            return;

        var deviceKey = await _deviceClient.LoginByIpAsync(
            _options.IpAddress,
            _options.Username,
            _options.Password
            );

        switch (state)
        {
            case ChoreAlertState.Ok:
                await _deviceClient.SetStateAsync(deviceKey, new TapoSetBulbState(
                    color: TapoColor.FromRgb(0, 255, 0),
                    deviceOn: true));
                break;

            case ChoreAlertState.Overdue:
                await _deviceClient.SetStateAsync(deviceKey, new TapoSetBulbState(
                    color: TapoColor.FromRgb(255, 0, 0),
                    deviceOn: true));
                break;
        }
    }
}
