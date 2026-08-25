using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace GPTConversationSplitter.App;

public partial class MainWindow
{
    // This expression is intentionally kept on the standard regex engine because it uses a fixed-width
    // negative lookbehind. RegexOptions.NonBacktracking does not support lookarounds and would throw while
    // MainWindow's static fields initialize, before the application window can appear.
    private static readonly Regex WindowsPathPattern = new(
        @"(?<![A-Za-z0-9_])(?:[A-Za-z]:\\|\\\\)(?:[^\r\n<>\""|?*]+)",
        RegexOptions.CultureInvariant);

    private void SaveRedactedLog_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Save Redacted Activity Log",
            Filter = "Text file (*.txt)|*.txt",
            FileName = $"GPT_Conversation_Splitter_Redacted_Activity_{DateTime.Now:yyyy-MM-dd_HHmmss}.txt"
        };
        if (dialog.ShowDialog(this) != true) return;

        var titleMap = _rows
            .Select(static row => row.Record.Title)
            .Distinct(StringComparer.Ordinal)
            .OrderByDescending(static title => title.Length)
            .Select(static (title, index) => new KeyValuePair<string, string>(title, $"Conversation {index + 1:D3}"))
            .ToArray();

        var sourcePath = _sourcePath;
        var sourceFileName = string.IsNullOrWhiteSpace(sourcePath) ? null : Path.GetFileName(sourcePath);

        var builder = new StringBuilder();
        builder.AppendLine($"{GPTConversationSplitter.Core.AppInfo.DisplayName} — Redacted Activity Log");
        builder.AppendLine($"Developed by {GPTConversationSplitter.Core.AppInfo.Developer}");
        builder.AppendLine($"Saved by explicit user request: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine("Conversation titles and local filesystem paths are pseudonymized. Performance, compatibility, memory, counts, and verification diagnostics are preserved.");

        foreach (var entry in _activityHistory)
        {
            var text = entry.Display;
            foreach (var pair in titleMap)
                text = text.Replace(pair.Key, pair.Value, StringComparison.Ordinal);

            if (!string.IsNullOrWhiteSpace(sourcePath))
                text = text.Replace(sourcePath, "<source-path>", StringComparison.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(sourceFileName))
                text = text.Replace(sourceFileName, "<source-file>", StringComparison.OrdinalIgnoreCase);

            text = WindowsPathPattern.Replace(text, "<local-path>");
            builder.AppendLine(text);
        }

        File.WriteAllText(dialog.FileName, builder.ToString(), new UTF8Encoding(false));
        _activity.Write("LOG", "Redacted activity log saved by explicit user request.", GPTConversationSplitter.Core.ActivityLevel.Success);
    }
}
