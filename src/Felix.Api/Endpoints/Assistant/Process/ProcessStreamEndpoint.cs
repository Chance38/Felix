using System.Text.Json;
using Felix.Infrastructure;
using Felix.Infrastructure.AI;

namespace Felix.Api.Endpoints.Assistant.Process;

public static class ProcessStreamEndpoint
{
    private static readonly JsonSerializerOptions JsonOptions = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static async Task HandleAsync(
        ProcessRequest request,
        IFelix felix,
        IRequestContext requestContext,
        HttpResponse httpResponse,
        CancellationToken cancellationToken)
    {
        if (request.Location != null)
        {
            requestContext.SetLocation(request.Location.Latitude, request.Location.Longitude);
        }

        httpResponse.ContentType = "text/event-stream";
        httpResponse.Headers.CacheControl = "no-cache";
        httpResponse.Headers["X-Accel-Buffering"] = "no";

        await foreach (var chunk in felix.ProcessStreamAsync(request.Message!, request.ConversationId, cancellationToken))
        {
            string data;

            if (chunk.IsDone)
                data = JsonSerializer.Serialize(new { type = "done", conversationId = chunk.ConversationId }, JsonOptions);
            else
                data = JsonSerializer.Serialize(new { type = "chunk", content = chunk.Content }, JsonOptions);

            await httpResponse.WriteAsync($"data: {data}\n\n", cancellationToken);
            await httpResponse.Body.FlushAsync(cancellationToken);
        }
    }
}
