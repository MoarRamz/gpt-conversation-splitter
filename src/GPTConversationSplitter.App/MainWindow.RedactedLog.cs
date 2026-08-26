using System.Text;
using System.Text.RegularExpressions;
using GPTConversationSplitter.Core;
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

    private static readonly Regex IndexTitlePattern = new(
        @"^\d+\s{2}(?<title>.+) — \d+ visible messages$",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    private static readonly Regex CompatibilityTitlePattern = new(
        @"^(?<title>.+): readable exports blocked until support is added",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    private static readonly Regex QuotedConversationValuePattern = new(
        @"(?i:(?:conversation(?: ID)?|selected conversation))\s+'(?<value>[^']+)'",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    private void RegisterSensitiveValuesFromActivity(ActivityEvent entry)
    {
        var message = entry.Message;

        if (entry.Category.Equals("INDEX", StringComparison.OrdinalIgnoreCase))
        {
            var indexMatch = IndexTitlePattern.Match(message);
            if (indexMatch.Success)
                _activity.RegisterTitle(indexMatch.Groups["title"].Value);
        }

        if (entry.Category.Equals("COMPAT", StringComparison.OrdinalIgnoreCase))
        {
            var compatibilityMatch = CompatibilityTitlePattern.Match(message);
            if (compatibilityMatch.Success)
                _activity.RegisterTitle(compatibilityMatch.Groups["title"].Value);
        }

        foreach (Match match in QuotedConversationValuePattern.Matches(message))
            _activity.RegisterIdentifier(match.Groups["value"].Value);

        foreach (Match match in WindowsPathPattern.Matches(message))
            _activity.RegisterPath(match.Value);
    }

    private void RebuildRedactionRegistryForCurrentState()
    {
        _activity.ClearSensitiveValues();
        if (!string.IsNullOrWhiteSpace(_sourcePath))
        {
            _activity.RegisterPath(_sourcePath);
            _activity.RegisterPath(Path.GetFileName(_sourcePath));
        }

        foreach (var row in _rows)
        {
            _activity.RegisterTitle(row.Record.Title);
            _activity.RegisterIdentifier(row.Record.Id);
        }
    }

    private void SaveRedactedLog_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Save Redacted Activity Log",
            Filter = "Text file (*.txt)|*.txt",
            FileName = $"LLM_Continuity_Toolkit_Redacted_Activity_{DateTime.Now:yyyy-MM-dd_HHmmss}.txt"
        };
        if (dialog.ShowDialog(this) != true) return;

        var sensitive = _activity.GetSensitiveSnapshot();
        var titleMap = sensitive.Titles
            .Select(static (title, index) => new KeyValuePair<string, string>(title, $"Conversation {index + 1:D3}"))
            .ToArray();

        var builder = new StringBuilder();
        builder.AppendLine($"{AppInfo.DisplayName} — Redacted Activity Log");
        builder.AppendLine($"Developed by {AppInfo.Developer}");
        builder.AppendLine($"Saved by explicit user request: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine("Conversation titles, conversation identifiers, and local filesystem values are pseudonymized. Performance, compatibility, memory, counts, and verification diagnostics are preserved.");

        foreach (var entry in _activityHistory)
        {
            var text = entry.Display;

            foreach (var pair in titleMap)
                text = text.Replace(pair.Key, pair.Value, StringComparison.Ordinal);

            foreach (var identifier in sensitive.Identifiers)
                text = text.Replace(identifier, "<conversation-id>", StringComparison.Ordinal);

            foreach (var path in sensitive.Paths)
                text = text.Replace(path, "<local-path>", StringComparison.OrdinalIgnoreCase);

            text = WindowsPathPattern.Replace(text, "<local-path>");
            builder.AppendLine(text);
        }

        File.WriteAllText(dialog.FileName, builder.ToString(), new UTF8Encoding(false));
        _activity.Write("LOG", "Redacted activity log saved by explicit user request.", ActivityLevel.Success);
    }
}
