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
            cache);

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
            cache);

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
}
