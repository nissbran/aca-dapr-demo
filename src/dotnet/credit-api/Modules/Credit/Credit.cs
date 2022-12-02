using System.Text.Json.Serialization;

namespace CreditApi.Modules.Credit;

public class Credit
{
    public string Id { get; init; }
    public string Name { get; set; }
    public ICollection<Transaction> Transactions { get; } = new List<Transaction>();
    [JsonIgnore]
    public IReadOnlyCollection<Transaction> NewTransactions => _newTransactions;

    private readonly List<Transaction> _newTransactions = new();

    public void AddTransaction(int value)
    {
        var newTransaction = new Transaction { Id = Guid.NewGuid().ToString(), Value = value };
        Transactions.Add(newTransaction);
        _newTransactions.Add(newTransaction);
    }
}

public class Transaction
{
    public string Id { get; init; }
    public int Value { get; init; }
}