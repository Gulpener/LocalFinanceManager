using Microsoft.JSInterop;

namespace LocalFinanceManager.Services;

public enum ClientOperatingSystem
{
    Unknown,
    Windows,
    MacOS,
    Linux
}

public interface IDeviceDetectionService
{
    Task<bool> IsTouchDeviceAsync();
    Task<ClientOperatingSystem> GetOperatingSystemAsync();
}

public class DeviceDetectionService : IDeviceDetectionService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<DeviceDetectionService> _logger;
    private bool? _isTouchDevice;
    private ClientOperatingSystem? _operatingSystem;

    public DeviceDetectionService(IJSRuntime jsRuntime, ILogger<DeviceDetectionService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task<bool> IsTouchDeviceAsync()
    {
        if (_isTouchDevice.HasValue)
        {
            return _isTouchDevice.Value;
        }

        try
        {
            _isTouchDevice = await _jsRuntime.InvokeAsync<bool>("localFinanceKeyboard.isTouchDevice");
            return _isTouchDevice.Value;
        }
        catch (JSException ex)
        {
            _logger.LogWarning(ex, "Unable to detect touch device state via JSInterop");
            _isTouchDevice = false;
            return false;
        }
    }

    public async Task<ClientOperatingSystem> GetOperatingSystemAsync()
    {
        if (_operatingSystem.HasValue)
        {
            return _operatingSystem.Value;
        }

        try
        {
            var os = await _jsRuntime.InvokeAsync<string>("localFinanceKeyboard.getOperatingSystem");
            _operatingSystem = os?.ToLowerInvariant() switch
            {
                "windows" => ClientOperatingSystem.Windows,
                "macos" => ClientOperatingSystem.MacOS,
                "linux" => ClientOperatingSystem.Linux,
                _ => ClientOperatingSystem.Unknown
            };

            return _operatingSystem.Value;
        }
        catch (JSException ex)
        {
            _logger.LogWarning(ex, "Unable to detect operating system via JSInterop");
            _operatingSystem = ClientOperatingSystem.Unknown;
            return ClientOperatingSystem.Unknown;
        }
    }
}