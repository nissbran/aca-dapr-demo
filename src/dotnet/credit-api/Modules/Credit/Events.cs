namespace CreditApi.Modules.Credit;

public record StartBookingEvent(string CreditId)
{    
    public string Type => nameof(StartBookingEvent);
}

public record BookingEvent(string CreditId, int Value, string Date, string? ETag)
{
    public string Type => nameof(BookingEvent);
}

public record CloseMonthEvent(string CreditId, int Month)
{
    public string Type => nameof(CloseMonthEvent);
}