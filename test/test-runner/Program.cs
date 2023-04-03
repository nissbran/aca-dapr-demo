using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

var azure = "inserturi";
var localCreditApi = "http://localhost:5010";
var localBooking = "http://localhost:5011";
var client = new HttpClient() { BaseAddress = new Uri(azure) };
var tasks = new Task[10];
var creditIds = new ConcurrentBag<string>();

for (int i = 0; i < 10; i++)
{
    var creditNumber = i;
    tasks[i] = Task.Run(async () =>
    {
        for (int j = 0; j < 1; j++)
        {
            var createCreditResponse = await client.PostAsJsonAsync("v1/credits/", new
            {
                Name = $"credit-{creditNumber}-{j}",
                StartDate = "2022-01-01"
            });

            if (createCreditResponse.StatusCode != HttpStatusCode.Created)
            {
                Console.WriteLine("Error: Credit not created");
                return;
            }
            
            var creditId = JsonSerializer.Deserialize<CreateCreditResponse>(await createCreditResponse.Content.ReadAsStringAsync())?.CreditId;
            if (creditId != null)
            {
                creditIds.Add(creditId);
            }

            var location = createCreditResponse.Headers.Location;
            for (int k = 0; k < 12; k++)
            {
                for (int l = 0; l < 10; l++)
                {
                    var response = await client.PostAsJsonAsync($"{location}/transactions", new
                    {
                        Value = 10,
                        TransactionDate = $"2022-{k+1:00}-{l+1:00}"
                    });

                    //await Task.Delay(1000);
                    //await Task.Delay(Random.Shared.Next(1000, 10000));
                }

                var closeCreditResponse = await client.PutAsJsonAsync($"{location}/close-month", new { });
            }
        }
    });
}

Task.WaitAll(tasks);

Console.WriteLine("Done sending waiting for enter to run verify");
Console.ReadLine();

var bookingClient = new HttpClient() { BaseAddress = new Uri(localBooking) };

foreach (var creditId in creditIds)
{
    for (int i = 0; i < 12; i++)
    {
        var month = i + 1;
        Console.WriteLine($"Verify id {creditId} and month {month}");
        var response = await bookingClient.GetAsync($"v1/bookings/{creditId}/month/{month}");
        if (!response.IsSuccessStatusCode)
            Console.WriteLine($"No OK status code for id {creditId} and month {month}");
        
        var bookingResponse = JsonSerializer.Deserialize<BookingMonthResponse>(await response.Content.ReadAsStringAsync());
        if (bookingResponse?.Total != 100)
        {
            Console.WriteLine($"Wrong result for id {creditId} and month {month}");
        }
    }
}

Console.WriteLine("Done");

public class CreateCreditResponse
{
    [JsonPropertyName("creditId")]
    public string CreditId { get; set; }
}


public class BookingMonthResponse
{
    [JsonPropertyName("bookings")]
    public ICollection<int> Bookings { get; set; } = new List<int>();
    [JsonPropertyName("total")]
    public int Total { get; set; }
    [JsonPropertyName("closed")]
    public bool Closed { get; set; }
}