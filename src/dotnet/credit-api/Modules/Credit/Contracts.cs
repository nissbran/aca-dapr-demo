using System.ComponentModel.DataAnnotations;

namespace CreditApi.Modules.Credit;

public class CreateCreditRequest
{
    [Required]
    public string Name { get; set; }
    public string StartDate { get; set; }
}
public class AddTransactionRequest
{
    [Required]
    public int Value { get; set; }
    
    public string TransactionDate { get; set; }
}

public class GetTransactionsResponse
{
    public int Count { get; set; }
}
