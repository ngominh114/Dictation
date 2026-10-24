using Dictation.Application.Import;

namespace Dictation.Application.Pipelines;

/// <summary>
/// Runs ordered steps in memory, skipping those whose guard is false. A derived pipeline lists its steps in
/// <see cref="ConfigureSteps"/>.
/// </summary>
public abstract class Pipeline<T>
{
    private IReadOnlyList<IPipelineStep<T>>? _steps;

    public IReadOnlyList<IPipelineStep<T>> Steps => _steps ??= BuildSteps();

    /// <summary>Adds the steps to <paramref name="steps"/> in the order they run.</summary>
    protected abstract void ConfigureSteps(List<IPipelineStep<T>> steps);

    /// <summary>Runs each step whose guard applies, in order. A step exception is wrapped in an <see cref="ImportException"/> naming the step.</summary>
    public async Task<T> RunAsync(T item, CancellationToken ct)
    {
        foreach (var step in Steps)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                if (!step.AppliesTo(item))
                {
                    continue;
                }

                item = await step.RunAsync(item, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ImportException($"Pipeline step '{step.GetType().Name}' failed.", ex);
            }
        }

        return item;
    }

    private IReadOnlyList<IPipelineStep<T>> BuildSteps()
    {
        var steps = new List<IPipelineStep<T>>();
        ConfigureSteps(steps);
        if (steps.Any(s => s is null))
        {
            throw new InvalidOperationException("Pipeline steps must not be null.");
        }

        return steps.AsReadOnly();
    }
}
