using System.Diagnostics.Metrics;
using OpenTelemetry.Metrics;

namespace BookingProcessor.Telemetry;

public class EventConsumedMetrics : IDisposable
{
    internal static readonly string InstrumentationName = "BookingProcessor.EventConsumed";
    internal static readonly string InstrumentationVersion = "0.1";

    private readonly Meter _meter;
    private readonly Counter<long> _startBookingCounter;
    private readonly ObservableGauge<long> _startBookingGauge;
    private readonly Counter<long> _transactionBookingCounter;
    private readonly ObservableGauge<long> _transactionBookingGauge;
    private readonly Counter<long> _closedMonthCounter;
    private readonly ObservableGauge<long> _closedMonthGauge;
    private long _startBookingEvents;
    private long _transactionBookingEvents;
    private long _closedMonthEvents;
    
    public EventConsumedMetrics()
    {
        _meter = new Meter(InstrumentationName, InstrumentationVersion);
        
        _startBookingCounter = _meter.CreateCounter<long>("events.start.booking.consumed");
        _startBookingGauge = _meter.CreateObservableGauge("events.start.booking.consumed",
            () => Interlocked.Exchange(ref _startBookingEvents, 0));
        
        _transactionBookingCounter = _meter.CreateCounter<long>("events.transaction.booking.consumed");
        _transactionBookingGauge = _meter.CreateObservableGauge("events.transaction.booking.consumed",
            () => Interlocked.Exchange(ref _transactionBookingEvents, 0));
        
        _closedMonthCounter = _meter.CreateCounter<long>("events.closed.month.consumed");
        _closedMonthGauge = _meter.CreateObservableGauge("events.closed.month.consumed",
            () => Interlocked.Exchange(ref _closedMonthEvents, 0));
    }
    
    public void IncrementStartBookingEvents()
    {
        _startBookingCounter.Add(1);
        Interlocked.Increment(ref _startBookingEvents);
    }
    
    public void IncrementTransactionBookingEvents()
    {
        _transactionBookingCounter.Add(1);
        Interlocked.Increment(ref _transactionBookingEvents);
    }
    
    public void IncrementClosedMonthEvents()
    {
        _closedMonthCounter.Add(1);
        Interlocked.Increment(ref _closedMonthEvents);
    }
    
    public void Dispose()
    {
        _meter.Dispose();
    }
}

public static class MeterProviderExtensions
{
    internal static MeterProviderBuilder AddConsumedEventsMetrics(this MeterProviderBuilder builder)
    {
        builder.AddMeter(EventConsumedMetrics.InstrumentationName);
        return builder.AddInstrumentation(provider => provider.GetRequiredService<EventConsumedMetrics>());
    }
}
