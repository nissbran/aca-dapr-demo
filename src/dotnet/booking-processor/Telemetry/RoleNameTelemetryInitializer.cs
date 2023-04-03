using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace BookingProcessor.Telemetry;

/// <summary>
/// This is used to set the application role name. The role name is used by the application map in application insights 
/// </summary>
public class RoleNameTelemetryInitializer : ITelemetryInitializer
{
    private readonly string _name;

    public RoleNameTelemetryInitializer(string name)
    {
        _name = name;
    }
    
    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.Cloud.RoleName = _name;
    }
}