using Microsoft.ApplicationInsights.DataContracts;
using Serilog.Events;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;

namespace BookingProcessor.Telemetry;

public class CustomTraceTelemetryConverter : TraceTelemetryConverter
{
    public override void ForwardPropertiesToTelemetryProperties(LogEvent logEvent, ISupportProperties telemetryProperties,
        IFormatProvider formatProvider)
    {
        base.ForwardPropertiesToTelemetryProperties(logEvent, telemetryProperties, formatProvider, false, false, false);
    }
}