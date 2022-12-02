using Carter;
using Dapr.Client;
using Serilog;

namespace CreditApi.Modules.Credit;

// ReSharper disable once UnusedType.Global
public class CreditModule : ICarterModule
{
    private const string StateStore = "creditstore";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("v1/credits");

        group.MapPost("", CreateCredit);
        group.MapGet("{id}", GetCredit);
        group.MapPost("{id}/transactions", AddTransaction);
        group.MapGet("{id}/transactions", GetTransactions);
    }

    private static async Task<IResult> CreateCredit(HttpContext context, CreateCreditRequest request, DaprClient client)
    {
        var newCredit = new Credit
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name
        };
        await client.SaveStateAsync(StateStore, newCredit.Id, newCredit);
        return TypedResults.Created($"v1/credits/{newCredit.Id}");
    }

    private static async Task<IResult> GetCredit(HttpContext context, string id, DaprClient client)
    {
        var credit = await client.GetStateAsync<Credit>(StateStore, id);
        if (credit == null)
            return TypedResults.NotFound();
        return TypedResults.Ok(credit);
    }

    private static async Task<IResult> AddTransaction(HttpContext context, string id, AddTransactionRequest request, DaprClient client)
    {
        var (credit, etag) = await client.GetStateAndETagAsync<Credit>(StateStore, id);
        if (credit == null)
            return TypedResults.NotFound();

        credit.AddTransaction(request.Value);

        if (await client.TrySaveStateAsync(StateStore, credit.Id, credit, etag, new StateOptions { Concurrency = ConcurrencyMode.FirstWrite }))
        {
            foreach (var transaction in credit.NewTransactions)
            {
                Log.Information("Sent booking message");
                var booking = new BookingEvent(credit.Id, transaction.Value, etag, nameof(BookingEvent));
                await client.PublishEventAsync("pubsub", "bookings", booking, new Dictionary<string, string>()
                    {
                        {"partitionKey", credit.Id}
                    }
                    );
            }
            return TypedResults.Ok();
        }
        else
        {
            return TypedResults.Conflict();
        }
    }

    private static async Task<IResult> GetTransactions(HttpContext context, string id, DaprClient client)
    {
        var credit = await client.GetStateAsync<Credit>(StateStore, id);

        if (credit == null)
            return TypedResults.NotFound();
        return TypedResults.Ok(new GetTransactionsResponse { Count = credit.Transactions.Count });
    }
}