using System.Diagnostics.Metrics;

namespace CreditApi.Modules.Credit;

internal class CreditMetrics : IDisposable
{
    internal static readonly string InstrumentationName = "Modules.Credit.Metrics";
    internal static readonly string InstrumentationVersion = "0.1";
    
    private readonly Meter _meter;
    private readonly Counter<long> _creditsCreatedCounter;
    private readonly ObservableGauge<long> _creditsCreatedGauge;
    private readonly Counter<long> _creditsTransactionValueAddedCounter;
    private readonly ObservableGauge<long> _creditsTransactionValueAddedGauge;
    private readonly Histogram<long> _creditsTransactionValueHistogram;
    private long _creditsCreated;
    private long _creditsTransactionValueSum;

    public CreditMetrics()
    {
        _meter = new Meter(InstrumentationName, InstrumentationVersion);
        _creditsCreatedCounter = _meter.CreateCounter<long>("credits.created");
        _creditsCreatedGauge = _meter.CreateObservableGauge("credits.created", 
            () => Interlocked.Exchange(ref _creditsCreated, 0));
        _creditsTransactionValueAddedCounter = _meter.CreateCounter<long>("credits.transaction.value.added");
        _creditsTransactionValueAddedGauge = _meter.CreateObservableGauge("credits.transaction.value.added", 
            () => Interlocked.Exchange(ref _creditsTransactionValueSum, 0), "SEK");
        _creditsTransactionValueHistogram = _meter.CreateHistogram<long>("credits.transaction.value.histogram");
    }
    
    public void IncrementCreditsCreated()
    {
        _creditsCreatedCounter.Add(1);
        Interlocked.Increment(ref _creditsCreated);
    }
    
    public void AddTransactionValue(long value, string currency)
    {
        _creditsTransactionValueHistogram.Record(value, new("currency", currency), new("type", "debit"));
        _creditsTransactionValueAddedCounter.Add(value);
        Interlocked.Add(ref _creditsTransactionValueSum, value);
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}
