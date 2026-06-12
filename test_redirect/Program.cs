using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class Program {
    static async Task Main() {
        var handler = new HttpClientHandler { AllowAutoRedirect = true };
        var client = new HttpClient(handler);
        var payload = new { MessageId = Guid.NewGuid(), MessageType = 0, ReportedByUserId = Guid.NewGuid(), Reason = "Test" };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        
        var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:5145/api/reports") { Content = content };
        
        try {
            Console.WriteLine("Sending POST request to: http://localhost:5145/api/reports");
            var response = await client.SendAsync(request);
            Console.WriteLine($"\n--- HTTP RESPONSE ---");
            Console.WriteLine($"Original URL: {request.RequestUri}");
            Console.WriteLine($"Response StatusCode: {(int)response.StatusCode} {response.StatusCode}");
            Console.WriteLine($"Final URL: {response.RequestMessage.RequestUri}");
            Console.WriteLine($"Final Method: {response.RequestMessage.Method}");
            var resStr = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Response Content: {resStr}");
        } catch (Exception ex) {
            Console.WriteLine($"\n--- EXCEPTION ---");
            Console.WriteLine($"Exception: {ex.GetType().Name} - {ex.Message}");
            if (ex.InnerException != null) Console.WriteLine($"Inner: {ex.InnerException.GetType().Name} - {ex.InnerException.Message}");
        }
    }
}
