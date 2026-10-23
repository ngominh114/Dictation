namespace Dictation.Application.Pipelines;

public interface IPipelineStep<T>
{
    /// <summary>Data-driven guard: skip this step when its work isn't needed for this item.</summary>
    bool AppliesTo(T item);

    Task<T> RunAsync(T item, CancellationToken ct);
}
