using System.Diagnostics.Metrics;

namespace BookingProcessor.Telemetry;

public static class EventConsumedMeter
{
    public static Meter Meter { get; } = new Meter("booking-processor");
    
    public static Counter<long> StartBookingCounter { get; } = Meter.CreateCounter<long>("event_start_booking_consumed");
    public static Counter<long> BookingCounter { get; } = Meter.CreateCounter<long>("event_booking_consumed");
    public static Counter<long> ClosedMonthCounter { get; } = Meter.CreateCounter<long>("event_closed_month_consumed");
}