using System;
using System.Text.Json;
using System.Net.Http.Json;
using System.Net.Http;
using System.Threading.Tasks;

class Program {
    static async Task Main() {
        var json = @"[ { ""name"": ""test"" } ]";
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var obj = await content.ReadFromJsonAsync<object>();
        var payload = new { topProducts = obj };
        Console.WriteLine(JsonSerializer.Serialize(payload));
    }
}
