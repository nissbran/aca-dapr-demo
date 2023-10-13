using System.Diagnostics;
using BookingProcessor.Contract;
using BookingProcessor.Telemetry;
using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;
using Serilog;
using Serilog.Context;

namespace BookingProcessor.Controllers;

[ApiController]
public class BookingEventController : ControllerBase
{
    private const string StateStore = "bookingstore";
    private const string SessionIdleTimeoutInSec = "20";
    private const string MaxConcurrentSessions = "2";

    [Topic("pubsub", "bookings", "event.data.type ==\"StartBookingEvent\"", 1)]
    [TopicMetadata("requireSessions", "true")]
    [TopicMetadata("sessionIdleTimeoutInSec", SessionIdleTimeoutInSec)]
    [TopicMetadata("maxConcurrentSessions", MaxConcurrentSessions)]
    [HttpPost("initialize-booking")]
    public async Task<IActionResult> HandleCreditCreated(StartBookingEvent startBooking, DaprClient client, EventConsumedMetrics metrics)
    {
        var creditId = startBooking.CreditId;
        Baggage.SetBaggage("creditId", creditId);
        Activity.Current?.AddTag("creditId", creditId);
        using var _ = LogContext.PushProperty("CreditId", creditId);
        
        Log.Information("New credit   - {CreditId}", startBooking.CreditId);

        await client.SaveStateAsync(StateStore, $"{startBooking.CreditId}-{1}", new BookingMonth(1));

        metrics.IncrementStartBookingEvents();

        await Task.Delay(new Random().Next(1000, 2000));

        return Ok();
    }

    [Topic("pubsub", "bookings", "event.data.type ==\"BookingEvent\"", 2)]
    [TopicMetadata("requireSessions", "true")]
    [TopicMetadata("sessionIdleTimeoutInSec", SessionIdleTimeoutInSec)]
    [TopicMetadata("maxConcurrentSessions", MaxConcurrentSessions)]
    [HttpPost("bookings")]
    public async Task<IActionResult> HandleBooking(BookingEvent booking, DaprClient client, EventConsumedMetrics metrics)
    {
        var creditId = booking.CreditId;
        Baggage.SetBaggage("creditId", creditId);
        Activity.Current?.AddTag("creditId", creditId);
        using var _ = LogContext.PushProperty("CreditId", creditId);

        Log.Information("Started      - {CreditId} -- value: {Value} -- tag: {ETag}", booking.CreditId, booking.Value,
            booking.ETag);

        var month = DateOnly.ParseExact(booking.Date, "yyyy-MM-dd").Month;
        var (bookingMonth, etag) =
            await client.GetStateAndETagAsync<BookingMonth>("bookingstore", $"{booking.CreditId}-{month}");

        await Task.Delay(new Random().Next(250, 500));

        bookingMonth ??= new BookingMonth(month);

        if (bookingMonth.Closed)
        {
            Log.Error("Tried to add transaction to closed month {@Booking}, sending to manual handling", booking);
            await client.PublishEventAsync("pubsub", "faulty-bookings", new FaultyBooking(booking.CreditId, booking.Value, booking.Date, month));
            return Ok();
        }
        else
        {
            bookingMonth.AddBooking(booking.Value, booking.ETag);
        }

        var result = await client.TrySaveStateAsync(StateStore, $"{booking.CreditId}-{month}", bookingMonth, etag);

        if (result == false)
        {
            Log.Warning("Failed to save booking for {Booking}, sending conflict", $"{booking.CreditId}-{month}");
            return Conflict();
        }

        metrics.IncrementTransactionBookingEvents();

        Log.Information("Processed    - {CreditId} -- tag: {ETag}", booking.CreditId, booking.ETag);
        return Ok();
    }

    [Topic("pubsub", "bookings", "event.data.type ==\"CloseMonthEvent\"", 3)]
    [TopicMetadata("requireSessions", "true")]
    [TopicMetadata("sessionIdleTimeoutInSec", SessionIdleTimeoutInSec)]
    [TopicMetadata("maxConcurrentSessions", MaxConcurrentSessions)]
    [HttpPost("close-month")]
    public async Task<IActionResult> CloseMonth(CloseMonthEvent closeMonth, DaprClient client, EventConsumedMetrics metrics)
    {
        var creditId = Baggage.GetBaggage("creditId");
        Activity.Current?.AddTag("creditId", creditId);
        using var _ = LogContext.PushProperty("CreditId", closeMonth.CreditId);
        
        var (bookingMonth, etag) = await client.GetStateAndETagAsync<BookingMonth>(StateStore, $"{closeMonth.CreditId}-{closeMonth.Month}");

        bookingMonth ??= new BookingMonth(closeMonth.Month);
        bookingMonth.Closed = true;

        await Task.Delay(new Random().Next(500, 2000));

        Log.Information("Closed month - {Id} -- Month: {Month} -- Sum: {Sum}",
            closeMonth.CreditId, bookingMonth.Month, bookingMonth.Total);

        var result = await client.TrySaveStateAsync(StateStore, $"{closeMonth.CreditId}-{closeMonth.Month}", bookingMonth, etag);

        if (result == false)
        {
            Log.Warning("Failed to save booking for {Booking}, sending conflict",
                $"{closeMonth.CreditId}-{closeMonth.Month}");
            return Conflict();
        }

        metrics.IncrementClosedMonthEvents();

        return Ok();
    }
}

public class BookingMonth
{
    public int Month { get; }
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public int Total => Bookings.Sum(booking => booking.Value);
    public bool Closed { get; set; }

    public BookingMonth(int month)
    {
        Month = month;
    }

    public void AddBooking(int value, string etag)
    {
        if (Bookings.Any(booking => booking.ETag == etag))
        {
            Log.Warning("Duplicate booking for {ETag}", etag);
            return;
        }

        Bookings.Add(new Booking { Value = value, ETag = etag });
    }
}

public class Booking
{
    public int Value { get; set; }
    public string ETag { get; set; }
}