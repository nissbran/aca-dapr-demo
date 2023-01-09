using BookingProcessor.Contract;
using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace BookingProcessor.Controllers;

[ApiController]
public class BookingEventController : ControllerBase
{
    private const string StateStore = "bookingstore";
    
    [Topic("pubsub", "bookings", "event.data.type ==\"StartBookingEvent\"", 1)]
    [HttpPost("initialize-booking")]
    public async Task<IActionResult> HandleCreditCreated(StartBookingEvent startBooking, DaprClient client)
    {
        Log.Information("New credit   - {Id}", startBooking.CreditId);
        
        await Task.Delay(new Random().Next(1000, 2000));

        await client.SaveStateAsync(StateStore, $"{startBooking.CreditId}-{1}", new BookingMonth(1));
        
        return Ok();
    }

    [Topic("pubsub", "bookings", "event.data.type ==\"BookingEvent\"", 2)]
    [HttpPost("bookings")]
    public async Task<IActionResult> HandleBooking(BookingEvent booking, DaprClient client)
    {
        Log.Information("Started      - {Id} -- tag: {ETag}", booking.CreditId, booking.ETag);
        
        await Task.Delay(new Random().Next(250, 500));
        
        var month = DateOnly.ParseExact(booking.Date, "yyyy-MM-dd").Month;
        var bookingMonth = await client.GetStateAsync<BookingMonth>("bookingstore", $"{booking.CreditId}-{month}") ??
                           new BookingMonth(month);

        if (bookingMonth.Closed)
        {
            Log.Error("Tried to add transaction to closed month {@Booking}", booking);
        }
        else
        {
            bookingMonth.Bookings.Add(booking.Value);
        }

        await client.SaveStateAsync(StateStore, $"{booking.CreditId}-{month}", bookingMonth);

        Log.Information("Processed    - {Id} -- tag: {ETag}", booking.CreditId, booking.ETag);
        return Ok();
    }

    [Topic("pubsub", "bookings", "event.data.type ==\"CloseMonthEvent\"", 3)]
    [HttpPost("close-month")]
    public async Task<IActionResult> CloseMonth(CloseMonthEvent closeMonth, DaprClient client)
    {
        await Task.Delay(new Random().Next(500, 2000));
        
        var bookingMonth = await client.GetStateAsync<BookingMonth>(StateStore, $"{closeMonth.CreditId}-{closeMonth.Month}") ??
                           new BookingMonth(closeMonth.Month);

        bookingMonth.Closed = true;

        Log.Information("Closed month - {Id} -- Month: {Month} -- Sum: {Sum}", 
            closeMonth.CreditId, bookingMonth.Month, bookingMonth.Total);
        await client.SaveStateAsync(StateStore, $"{closeMonth.CreditId}-{closeMonth.Month}", bookingMonth);
        return Ok();
    }
}

public class BookingMonth
{
    public int Month { get; }
    public ICollection<int> Bookings { get; set; } = new List<int>();
    public int Total => Bookings.Sum();
    public bool Closed { get; set; }

    public BookingMonth(int month)
    {
        Month = month;
    }
}