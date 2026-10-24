using MediatR;

namespace Dictation.Tests.Support;

/// <summary>Records each request sent and answers it with <paramref name="response"/>.</summary>
public sealed class StubSender(object? response) : ISender
{
    public List<object> Sent { get; } = [];

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        Sent.Add(request);
        return Task.FromResult((TResponse)response!);
    }

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest =>
        throw new NotSupportedException();

    public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
