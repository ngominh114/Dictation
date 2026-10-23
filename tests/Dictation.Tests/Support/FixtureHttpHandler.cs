using System.Collections.Concurrent;
using System.Text;

namespace Dictation.Tests.Support;

/// <summary>Answers every HTTP request with <c>Fixtures/{provider}/{fixture}</c> and records the request URLs. No network.</summary>
public sealed class FixtureHttpHandler(string provider, string fixture) : HttpMessageHandler
{
    private readonly string body = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", provider, fixture));

    public ConcurrentQueue<Uri> Requests { get; } = new();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Enqueue(request.RequestUri!);
        return Task.FromResult(new HttpResponseMessage { Content = new StringContent(body, Encoding.UTF8, "application/json") });
    }
}
