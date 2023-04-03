using System.Diagnostics.Metrics;

namespace CreditApi.Telemetry;

public static class CreditsChangedMeter
{
    public static Meter Meter { get; } = new Meter("credit-api");
    
    public static Counter<long> CreditsCreatedCounter { get; } = Meter.CreateCounter<long>("credits_created");
    public static Counter<long> CreditsTransactionValueAdded { get; } = Meter.CreateCounter<long>("credits_transaction_value_added", "SEK");
}