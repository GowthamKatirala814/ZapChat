using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var client = new HttpClient();
        var payload = new { MessageId = System.Guid.NewGuid(), MessageType = 0, ReportedByUserId = System.Guid.NewGuid(), Reason = "Test" };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        try {
            var response = await client.PostAsync("http://localhost:5145/api/reports", content);
            System.Console.WriteLine($"StatusCode: {response.StatusCode}");
            var resStr = await response.Content.ReadAsStringAsync();
            System.Console.WriteLine($"Response: {resStr}");
        } catch (System.Exception ex) {
            System.Console.WriteLine($"Exception: {ex.Message}");
        }
    }
}
