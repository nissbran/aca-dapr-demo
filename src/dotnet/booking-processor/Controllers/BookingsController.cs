using Dapr.Client;
using Microsoft.AspNetCore.Mvc;

namespace BookingProcessor.Controllers;

[ApiController]
public class BookingsController : ControllerBase
{
    private const string StateStore = "bookingstore";
    
    [HttpGet("v1/bookings/{id}/month/{month:int}")]
    public async Task<IActionResult> GetBookingForCreditAndMonth(string id, int month, [FromServices]DaprClient client)
    {
        var bookingMonth = await client.GetStateAsync<BookingMonth>(StateStore, $"{id}-{month}");

        if (bookingMonth == null)
            return NotFound();
        return Ok(bookingMonth);
    }
}