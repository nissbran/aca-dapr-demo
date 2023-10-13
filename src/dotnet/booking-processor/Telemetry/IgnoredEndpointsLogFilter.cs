using Serilog.Core;
using Serilog.Events;

namespace BookingProcessor.Telemetry;

public class IgnoredEndpointsLogFilter : ILogEventFilter
{
    public bool IsEnabled(LogEvent logEvent)
    {
        if (logEvent.Properties.TryGetValue("RequestPath", out var requestPath))
        {
            if (requestPath.ToString().Contains("/metrics") || requestPath.ToString().Contains("/healthz"))
                return false;
        }

        return true;
    }
}