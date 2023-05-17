namespace BookingProcessor.Contract;

public record StartBookingEvent(string CreditId, string Type);
public record BookingEvent(string CreditId, int Value, string Date, string ETag, string Type);
public record CloseMonthEvent(string CreditId, int Month, string Type);

public record FaultyBooking(string CreditId, int Value, string Date, int Month)
{
    public string Type => nameof(FaultyBooking);
}