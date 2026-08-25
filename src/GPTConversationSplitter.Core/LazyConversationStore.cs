namespace GPTConversationSplitter.Core;

public sealed class LazyConversationStore
{
    private readonly ChatExportReader _reader;

    public LazyConversationStore(ActivitySink activity) => _reader = new ChatExportReader(activity);

    public Task<ImportResult> ReadMetadataAsync(
        string sourcePath,
        IProgress<ImportProgress>? progress = null,
        CancellationToken cancellationToken = default)
        => _reader.ReadMetadataAsync(sourcePath, progress, cancellationToken);

    public Task<IReadOnlyList<ConversationRecord>> HydrateSelectedAsync(
        string sourcePath,
        IReadOnlyList<ConversationRecord> selectedMetadata,
        CancellationToken cancellationToken = default)
        => _reader.HydrateSelectedAsync(sourcePath, selectedMetadata, cancellationToken);
}
