using System.Net;
using System.Text;

namespace Dictation.Tests.Support;

/// <summary>Answers every request with <paramref name="respond"/>(absolute URL), so a test controls the remote API.</summary>
public sealed class StubHttpHandler(Func<string, (HttpStatusCode Status, string Body)> respond) : HttpMessageHandler
{
    public HttpClient CreateClient() => new(this);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var (status, body) = respond(request.RequestUri!.AbsoluteUri);
        return Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        });
    }
}
