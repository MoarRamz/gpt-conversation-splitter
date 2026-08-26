using System.Runtime.CompilerServices;
using GPTConversationSplitter.Core;

internal static class V23RegressionGuards
{
    [ModuleInitializer]
    internal static void Run()
    {
        RunFailedImportRedactionRegression();
        RunRenamedStagingCancellationRegression();
        VerifyCiRuntimeLegalAssets();
    }

    private static void RunFailedImportRedactionRegression()
    {
        const string title = "Synthetic Private Conversation";
        const string id = "synthetic-private-conversation-id";
        const string sourcePath = @"C:\Users\SyntheticUser\Downloads\synthetic-chat-export.zip";
        const string sourceFile = "synthetic-chat-export.zip";

        var sink = new ActivitySink();
        var history = new List<ActivityEvent>();
        sink.Activity += (_, entry) =>
        {
            DiagnosticRedactor.RegisterSensitiveValues(sink, entry);
            history.Add(entry);
        };

        // The UI registers the chosen source path immediately, before indexing completes.
        sink.RegisterPath(sourcePath);
        sink.RegisterPath(sourceFile);

        // Simulate a partially indexed import that later fails and therefore never populates UI rows.
        sink.Write("IMPORT", $"Opening ChatGPT export: {sourceFile}");
        sink.Write("INDEX", $"1  {title} — 42 visible messages");
        sink.Write("COMPAT", $"Duplicate ChatGPT conversation ID '{id}' was found. Import stopped because later lazy hydration would be ambiguous.", ActivityLevel.Error);
        sink.Write("PERF", "Transcript indexing completed in 1.23 s", ActivityLevel.Performance);

        var sensitive = sink.GetSensitiveSnapshot();
        var redacted = string.Join(Environment.NewLine, history.Select(entry => DiagnosticRedactor.Redact(entry.Display, sensitive)));

        Require(!redacted.Contains(title, StringComparison.Ordinal), "Failed-import redaction leaked a conversation title.");
        Require(!redacted.Contains(id, StringComparison.Ordinal), "Failed-import redaction leaked a stable conversation identifier.");
        Require(!redacted.Contains(sourcePath, StringComparison.OrdinalIgnoreCase), "Failed-import redaction leaked an absolute source path.");
        Require(!redacted.Contains(sourceFile, StringComparison.OrdinalIgnoreCase), "Failed-import redaction leaked the source filename.");
        Require(redacted.Contains("Conversation 001", StringComparison.Ordinal), "Failed-import redaction did not preserve a conversation pseudonym.");
        Require(redacted.Contains("<conversation-id>", StringComparison.Ordinal), "Failed-import redaction did not preserve an identifier placeholder.");
        Require(redacted.Contains("<local-path>", StringComparison.Ordinal), "Failed-import redaction did not preserve a filesystem placeholder.");
        Require(redacted.Contains("42 visible messages", StringComparison.Ordinal), "Failed-import redaction removed useful message-count diagnostics.");
        Require(redacted.Contains("1.23 s", StringComparison.Ordinal), "Failed-import redaction removed useful performance diagnostics.");
    }

    private static void RunRenamedStagingCancellationRegression()
    {
        var tempRoot = Path.GetTempPath();
        var output = Path.Combine(tempRoot, "llm-continuity-cancel-regression-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
        var before = Directory.EnumerateDirectories(tempRoot, "llm-continuity-*").ToHashSet(StringComparer.OrdinalIgnoreCase);

        try
        {
            var rows = new[]
            {
                NewRecord("cancel-v23-a", "Cancel V23 A", 1_702_000_000),
                NewRecord("cancel-v23-b", "Cancel V23 B", 1_702_100_000)
            };

            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();

            try
            {
                new ExportService(new ActivitySink())
                    .ExportAsync(rows, ExportFormat.GptContinuationMarkdown, output, cancellationToken: cancelled.Token)
                    .GetAwaiter().GetResult();
                throw new InvalidOperationException("Cancelled v2.3 bundle export unexpectedly completed.");
            }
            catch (OperationCanceledException)
            {
                // Expected.
            }

            var after = Directory.EnumerateDirectories(tempRoot, "llm-continuity-*").ToHashSet(StringComparer.OrdinalIgnoreCase);
            after.ExceptWith(before);
            after.Remove(output);

            Require(after.Count == 0, "Cancelled v2.3 export left a renamed staging directory behind.");
            Require(!Directory.EnumerateFiles(output).Any(), "Cancelled v2.3 export left output or temporary files behind.");
        }
        finally
        {
            try { Directory.Delete(output, recursive: true); } catch { }
        }
    }

    private static void VerifyCiRuntimeLegalAssets()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), "true", StringComparison.OrdinalIgnoreCase))
            return;

        var candidateRoots = new List<string>();
        var configuredRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        if (!string.IsNullOrWhiteSpace(configuredRoot))
            candidateRoots.Add(configuredRoot);

        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var entry in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            try
            {
                if (File.Exists(Path.Combine(entry, "dotnet.exe")) || File.Exists(Path.Combine(entry, "dotnet")))
                    candidateRoots.Add(entry);
            }
            catch
            {
                // Ignore malformed PATH entries and continue searching.
            }
        }

        var dotnetRoot = candidateRoots
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(root => Directory.Exists(root)
                && Directory.EnumerateFiles(root, "*", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileName)
                    .Any(static name => name?.Equals("LICENSE.txt", StringComparison.OrdinalIgnoreCase) == true));

        Require(!string.IsNullOrWhiteSpace(dotnetRoot),
            "GitHub Actions could not locate the .NET root that exposes LICENSE.txt required by stable packaging.");

        var files = Directory.EnumerateFiles(dotnetRoot!, "*", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .ToArray();

        Require(files.Any(static name => name!.Equals("LICENSE.txt", StringComparison.OrdinalIgnoreCase)),
            $"Pinned Windows .NET installation under '{dotnetRoot}' does not expose LICENSE.txt required by stable packaging.");
        Require(files.Any(static name => name!.Equals("ThirdPartyNotices.txt", StringComparison.OrdinalIgnoreCase)
            || name.Equals("THIRD-PARTY-NOTICES.TXT", StringComparison.OrdinalIgnoreCase)),
            $"Pinned Windows .NET installation under '{dotnetRoot}' does not expose ThirdPartyNotices required by stable packaging.");
    }

    private static ConversationRecord NewRecord(string id, string title, double created)
        => new()
        {
            Id = id,
            Title = title,
            CreateTimeRaw = created,
            UpdateTimeRaw = created + 100,
            Messages = new[] { new ConversationMessage(1, "user", "Synthetic cancellation fixture", created + 1, 0) }
        };

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
