using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CreditApi.Modules.Credit;

public class CreateCreditRequest
{
    [Required]
    public string? Name { get; set; }
    public string? StartDate { get; set; }
}

public class AddTransactionRequest
{
    [Required]
    public int Value { get; set; }
    [Required]
    public string? Currency { get; set; }
    
    public string? TransactionDate { get; set; }
}

public class GetTransactionsResponse
{
    public int Count { get; set; }
}


public class GetInterestRateResponse
{
    [JsonPropertyName("interest_rate")]
    public decimal InterestRate { get; set; }
}

public class GetCurrencyConversionRateResponse
{
    [JsonPropertyName("conversion_rate")]
    public decimal ConversionRate { get; set; }
}