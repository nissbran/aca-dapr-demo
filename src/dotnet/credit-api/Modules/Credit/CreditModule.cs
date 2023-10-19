using System.Diagnostics;
using Dapr.Client;
using OpenTelemetry;
using Serilog;

namespace CreditApi.Modules.Credit;

public static class CreditModule
{
    private const string StateStore = "creditstore";

    public static void MapRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("v1/credits");

        group.MapPost("", CreateCredit);
        group.MapGet("{id}", GetCredit);
        group.MapPost("{id}/transactions", AddTransaction);
        group.MapGet("{id}/transactions", GetTransactions);
        group.MapPut("{id}/close-month", CloseMonth);
    }

    private static async Task<IResult> CreateCredit(CreateCreditRequest request, DaprClient client, CreditMetrics metrics)
    {
        var creditId = Guid.NewGuid().ToString();
        
        Baggage.SetBaggage("creditId", creditId);
        Activity.Current?.AddTag("creditId", creditId);
        
        var newCredit = new Credit
        {
            Id = creditId,
            Name = request.Name,
            InterestRate = await GetInterestRate(client),
            CurrentMonth = DateOnly.ParseExact(request.StartDate, "yyyy-MM-dd")
        };

        await client.SaveStateAsync(StateStore, newCredit.Id, newCredit, metadata: new Dictionary<string, string>()
        {
            { "partitionKey", newCredit.Id }
        });
        var startBooking = new StartBookingEvent(newCredit.Id);
        await client.PublishEventAsync("pubsub", "bookings", startBooking, new Dictionary<string, string>()
        {
            { "partitionKey", newCredit.Id }, { "SessionId", newCredit.Id}
        });
        
        metrics.IncrementCreditsCreated();
        
        return TypedResults.Created($"v1/credits/{newCredit.Id}", new
        {
            CreditId = newCredit.Id
        });
    }
    
    private static async Task<decimal> GetInterestRate(DaprClient client)
    {
        try
        {
            var interestRateResponse = await client.InvokeMethodAsync<GetInterestRateResponse>(HttpMethod.Get,"interest-rate-api", "v1/interest-rates");
            return interestRateResponse.InterestRate;
        }
        catch (Exception e)
        {
            Log.Warning("Failed to get interest rate, using default value");
            return 0.1m;
        }
    }

    private static async Task<IResult> GetCredit(string id, DaprClient client)
    {
        Baggage.SetBaggage("creditId", id);
        Activity.Current?.AddTag("creditId", id);
        
        var (credit, _) = await GetCreditState(client, id);
        if (credit == null)
            return TypedResults.NotFound();
        return TypedResults.Ok(credit);
    }

    private static async Task<IResult> AddTransaction(string id, AddTransactionRequest request, DaprClient client, CreditMetrics metrics)
    {
        Baggage.SetBaggage("creditId", id);
        Activity.Current?.AddTag("creditId", id);
        
        var (credit, etag) = await GetCreditState(client, id);
        if (credit == null)
            return TypedResults.NotFound();

        var transactionDate = DateOnly.ParseExact(request.TransactionDate, "yyyy-MM-dd");

        if (request.Currency != null && request.Currency != "SEK")
        {
            var conversionRate = await GetCurrencyRate(client, request.Currency);
            request.Value = (int) Math.Round(request.Value * conversionRate);
        }
        
        credit.AddTransaction(request.Value, transactionDate);

        if (await SaveCreditState(client, credit, etag))
        {
            foreach (var transaction in credit.NewTransactions)
            {
                metrics.AddTransactionValue(transaction.Value, "SEK");

                //await Task.Delay(100);
                
                Log.Information("Sent booking message");
                var booking = new BookingEvent(credit.Id, transaction.Value, transaction.TransactionDate.ToString("yyyy-MM-dd"), etag);
                await client.PublishEventAsync("pubsub", "bookings", booking, new Dictionary<string, string>()
                {
                    { "partitionKey", credit.Id }, { "SessionId", credit.Id}
                });
            }

            return TypedResults.Ok();
        }
        else
        {
            return TypedResults.Conflict();
        }
    }
    
    private static async Task<decimal> GetCurrencyRate(DaprClient client, string fromCurrency, string toCurrency = "SEK")
    {
        try
        {
            var conversionRateResponse = await client.InvokeMethodAsync<GetCurrencyConversionRateResponse>(HttpMethod.Get,"currency-rate-api", $"v1/currency-rate/from/{fromCurrency}/to/{toCurrency}");
            return conversionRateResponse.ConversionRate;
        }
        catch (Exception e)
        {
            Log.Warning(e,"Failed to get currency conversion rate, using default value");
            return 1;
        }
    }

    private static async Task<IResult> GetTransactions(string id, DaprClient client)
    {
        Baggage.SetBaggage("creditId", id);
        Activity.Current?.AddTag("creditId", id);
        
        var (credit, _) = await GetCreditState(client, id);
        if (credit == null)
            return TypedResults.NotFound();
        return TypedResults.Ok(new GetTransactionsResponse { Count = credit.Transactions.Count });
    }

    private static async Task<IResult> CloseMonth(string id, DaprClient client)
    {
        Baggage.SetBaggage("creditId", id);
        Activity.Current?.AddTag("creditId", id);
        
        var (credit, etag) = await GetCreditState(client, id);
        if (credit == null)
            return TypedResults.NotFound();

        var month = credit.CloseCurrentMonth();

        if (await SaveCreditState(client, credit, etag))
        {
            var closeMonth = new CloseMonthEvent(credit.Id, month);
            await client.PublishEventAsync("pubsub", "bookings", closeMonth, new Dictionary<string, string>()
            {
                { "partitionKey", credit.Id }, { "SessionId", credit.Id}
            });
            return TypedResults.Ok();
        }
        else
        {
            return TypedResults.Conflict();
        }
    }

    private static async Task<(Credit?, string?)> GetCreditState(DaprClient client, string id)
    {
        var (credit, etag) = await client.GetStateAndETagAsync<Credit>(StateStore, id,
            metadata: new Dictionary<string, string>()
            {
                { "partitionKey", id }
            });
        return (credit, etag);
    }

    private static async Task<bool> SaveCreditState(DaprClient client, Credit credit, string? etag)
    {
        return await client.TrySaveStateAsync(StateStore, credit.Id, credit, etag,
            new StateOptions { Concurrency = ConcurrencyMode.FirstWrite },
            new Dictionary<string, string>()
            {
                { "partitionKey", credit.Id }
            });
    }
}