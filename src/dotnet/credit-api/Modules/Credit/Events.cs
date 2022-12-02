namespace CreditApi.Modules.Credit;

public record BookingEvent(string CreditId, int Value, string ETag, string Type);