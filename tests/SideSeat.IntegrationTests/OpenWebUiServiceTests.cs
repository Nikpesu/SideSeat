using System.Security.Claims;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SideSeat.Models.Ai;
using SideSeat.Services;

namespace SideSeat.IntegrationTests;

public class OpenWebUiServiceTests
{
    [Fact]
    public async Task ChatAsync_SendsInstructionsAndAllContextInOneJsonSystemMessage()
    {
        string? requestJson = null;
        using var handler = new RecordingHandler(async request =>
        {
            requestJson = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"choices":[{"message":{"content":"U redu."}}]}""",
                    Encoding.UTF8,
                    "application/json")
            };
        });
        using var httpClient = new HttpClient(handler);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new OpenWebUiService(
            httpClient,
            Options.Create(new OpenWebUiOptions
            {
                ApiType = "OpenWebUi",
                BaseUrl = "https://ai.example.test",
                ApiKey = "test-key",
                Model = "test-model"
            }),
            cache,
            new FakeAiToolService());

        var response = await service.ChatAsync(
            new AiChatRequest
            {
                Messages =
                [
                    new AiChatMessage
                    {
                        Role = "user",
                        Content = "Prikaži moje vožnje."
                    }
                ]
            },
            """{"user":{"id":2,"saldo":50},"ridesAsPassenger":[{"id":7}]}""",
            new ClaimsPrincipal(new ClaimsIdentity()),
            CancellationToken.None);

        Assert.Equal("U redu.", response.Message);
        Assert.NotNull(requestJson);

        using var requestDocument = JsonDocument.Parse(requestJson);
        var messages = requestDocument.RootElement.GetProperty("messages");
        Assert.Equal(2, messages.GetArrayLength());
        Assert.Equal("system", messages[0].GetProperty("role").GetString());

        var systemContent = messages[0].GetProperty("content").GetString();
        Assert.NotNull(systemContent);
        Assert.DoesNotContain("SIDESEAT_CONTEXT_BEGIN", systemContent);

        using var contextDocument = JsonDocument.Parse(systemContent);
        Assert.Equal("sideseat.ai-context.v1", contextDocument.RootElement.GetProperty("schema").GetString());
        Assert.Equal(
            2,
            contextDocument.RootElement
                .GetProperty("data")
                .GetProperty("user")
                .GetProperty("id")
                .GetInt32());
        Assert.Equal(
            7,
            contextDocument.RootElement
                .GetProperty("data")
                .GetProperty("ridesAsPassenger")[0]
                .GetProperty("id")
                .GetInt32());
        Assert.True(
            contextDocument.RootElement
                .GetProperty("assistant")
                .GetProperty("rules")
                .GetArrayLength() > 0);
    }

    [Theory]
    [InlineData("OpenWebUi", "/api/models", "/api/chat/completions")]
    [InlineData("DeepSeek", "/models", "/chat/completions")]
    public async Task ChatAsync_UsesProviderSpecificEndpoints(
        string apiType,
        string modelsPath,
        string chatPath)
    {
        var requestedPaths = new List<string>();
        string? chatRequestJson = null;
        using var handler = new RecordingHandler(async request =>
        {
            requestedPaths.Add(request.RequestUri!.AbsolutePath);
            if (request.Method == HttpMethod.Get)
            {
                return JsonResponse("""{"data":[{"id":"provider-model"}]}""");
            }

            chatRequestJson = await request.Content!.ReadAsStringAsync();
            return JsonResponse("""{"choices":[{"message":{"content":"Radi."}}]}""");
        });
        using var httpClient = new HttpClient(handler);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new OpenWebUiService(
            httpClient,
            Options.Create(new OpenWebUiOptions
            {
                ApiType = apiType,
                BaseUrl = "https://ai.example.test",
                ApiKey = "test-key"
            }),
            cache,
            new FakeAiToolService());

        var response = await service.ChatAsync(
            new AiChatRequest
            {
                Messages =
                [
                    new AiChatMessage
                    {
                        Role = "user",
                        Content = "Test."
                    }
                ]
            },
            "{}",
            new ClaimsPrincipal(new ClaimsIdentity()),
            CancellationToken.None);

        Assert.Equal("Radi.", response.Message);
        Assert.Equal("provider-model", response.Model);
        Assert.Equal([modelsPath, chatPath], requestedPaths);
        Assert.NotNull(chatRequestJson);

        using var requestDocument = JsonDocument.Parse(chatRequestJson);
        Assert.Equal(
            "provider-model",
            requestDocument.RootElement.GetProperty("model").GetString());
    }

    [Fact]
    public async Task ChatAsync_ExecutesToolCallAndReturnsFinalAnswer()
    {
        var requestBodies = new List<string>();
        var responseNumber = 0;
        var tools = new FakeAiToolService();
        using var handler = new RecordingHandler(async request =>
        {
            requestBodies.Add(await request.Content!.ReadAsStringAsync());
            responseNumber++;
            return responseNumber == 1
                ? JsonResponse(
                    """
                    {"choices":[{"message":{"role":"assistant","content":null,"tool_calls":[{"id":"call-1","type":"function","function":{"name":"get_balance","arguments":"{\"transactionLimit\":5}"}}]}}]}
                    """)
                : JsonResponse(
                    """{"choices":[{"message":{"role":"assistant","content":"Rijeka $\\rightarrow$ Zagreb | [Detalji](/Voznja/Details/7)"}}]}""");
        });
        using var httpClient = new HttpClient(handler);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new OpenWebUiService(
            httpClient,
            Options.Create(new OpenWebUiOptions
            {
                ApiType = "DeepSeek",
                BaseUrl = "https://api.deepseek.test",
                ApiKey = "test-key",
                Model = "test-model"
            }),
            cache,
            tools);

        var response = await service.ChatAsync(
            new AiChatRequest
            {
                Messages =
                [
                    new AiChatMessage
                    {
                        Role = "user",
                        Content = "Koliki mi je saldo?"
                    }
                ]
            },
            """{"sitemap":[{"label":"Moj saldo","path":"/Korisnik/Saldo"}]}""",
            new ClaimsPrincipal(new ClaimsIdentity()),
            CancellationToken.None);

        Assert.Equal(
            "Rijeka → Zagreb | [Detalji](/Voznja/Details/7)",
            response.Message);
        Assert.Equal("get_balance", tools.LastToolName);
        Assert.Equal(2, requestBodies.Count);

        using var secondRequest = JsonDocument.Parse(requestBodies[1]);
        var messages = secondRequest.RootElement.GetProperty("messages");
        Assert.Contains(
            messages.EnumerateArray(),
            message =>
                message.GetProperty("role").GetString() == "tool" &&
                message.GetProperty("tool_call_id").GetString() == "call-1");
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            responseFactory(request);
    }

    private sealed class FakeAiToolService : IAiToolService
    {
        public IReadOnlyList<object> Definitions { get; } =
        [
            new
            {
                type = "function",
                function = new
                {
                    name = "get_balance",
                    description = "Test",
                    parameters = new
                    {
                        type = "object",
                        properties = new { }
                    }
                }
            }
        ];

        public string? LastToolName { get; private set; }

        public Task<string> ExecuteAsync(
            string toolName,
            string argumentsJson,
            ClaimsPrincipal principal,
            CancellationToken cancellationToken)
        {
            LastToolName = toolName;
            return Task.FromResult("""{"balance":50}""");
        }
    }
}
