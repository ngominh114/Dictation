namespace Dictation.Application.Import;

/// <summary>
/// An import failure: a problem an adapter reports (error status, empty response), or a pipeline step that
/// failed, wrapping the step's error with its name in the message (AD-5).
/// </summary>
public sealed class ImportException(string message, Exception? inner = null) : Exception(message, inner);
