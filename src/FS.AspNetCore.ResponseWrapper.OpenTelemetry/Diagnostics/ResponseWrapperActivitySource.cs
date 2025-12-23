using System.Diagnostics;

namespace FS.AspNetCore.ResponseWrapper.OpenTelemetry.Diagnostics;

/// <summary>
/// Activity source for Response Wrapper telemetry
/// </summary>
public static class ResponseWrapperActivitySource
{
    private static ActivitySource? _activitySource;

    /// <summary>
    /// Gets or initializes the ActivitySource for Response Wrapper
    /// </summary>
    public static ActivitySource Instance
    {
        get
        {
            _activitySource ??= new ActivitySource("FS.AspNetCore.ResponseWrapper", "10.0.0");
            return _activitySource;
        }
    }

    /// <summary>
    /// Creates a new activity source with custom name and version
    /// </summary>
    public static ActivitySource Create(string name, string version = "10.0.0")
    {
        return new ActivitySource(name, version);
    }
}
