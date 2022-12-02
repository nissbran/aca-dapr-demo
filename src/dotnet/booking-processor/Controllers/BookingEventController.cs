using Dapr;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace BookingProcessor.Controllers;

[ApiController]
public class BookingEventController : ControllerBase
{
    [Topic("pubsub", "bookings", "event.data.type ==\"BookingEvent\"", 1)]
    [HttpPost("bookings")]
    public async Task<IActionResult> Deposit(BookingEvent booking)
    {
        Log.Information("Started   - {Id} -- tag: {ETag}",booking.CreditId, booking.ETag);        
        await Task.Delay(new Random().Next(1000, 5000));
        Log.Information("Processed - {Id} -- tag: {ETag}",booking.CreditId, booking.ETag);
        return Ok();
    }
}

public record BookingEvent(string CreditId, int Value, string ETag, string Type);