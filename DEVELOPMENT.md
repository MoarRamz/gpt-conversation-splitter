# Development Notes

## Design constraints

LLM Continuity Toolkit is intentionally a small, portable, local-first Windows utility.

The current supported input is the official ChatGPT data-export format. The LLM product name describes the problem domain and must not be used to imply unimplemented compatibility with other providers.

Avoid adding persistent infrastructure unless there is a compelling functional requirement. In particular, do not introduce:

- conversation caches;
- databases;
- application telemetry or analytics;
- cloud synchronization;
- background services;
- automatic update daemons;
- account/API integration;
- unnecessary third-party framework/runtime dependencies.

Prefer temporary computation over persistent storage when the tradeoff is reasonable.

## Current architecture

Version 2.3 retains the accepted v2.2 application architecture and uses:

- C#;
- WPF;
- .NET 10;
- `System.Text.Json`;
- `System.IO.Compression`;
- `System.Security.Cryptography`;
- `Task` / `CancellationToken` / `IProgress`;
- minimal third-party dependency surface.

The import path streams `conversations.json`, reconstructs only the active `current_node` path for each conversation, classifies visibility structurally, derives a lightweight retained metadata index, and discards transcript bodies/raw conversation state as soon as practical.

Readable export directly rehydrates selected transcripts from the original source in one scan. Complete Conversation JSON streams selected original records from the source and verifies the canonical raw-record fingerprint before output is accepted.

Internal source project/namespace identifiers retain the historical `GPTConversationSplitter` name. These are implementation identifiers, not public branding, and should not be renamed solely for cosmetic consistency because doing so would create broad nonfunctional churn across source and tests.

The established `GPT_SPLITTER_TURN` continuation framing marker is likewise a stable legacy file-format protocol identifier and should remain unchanged unless a deliberate format-version migration is justified.

## Optimization policy

Future performance work should be driven by measured regressions or materially improved resource behavior. Prefer:

- fewer retained allocations;
- bounded memory usage;
- early release of temporary buffers;
- operation-boundary memory telemetry;
- measurable startup/import/export improvements;
- correctness over marginal throughput gains.

Do not introduce unsafe code, custom allocators, NativeAOT/trimming complexity, a parallel parser, persistent preloading, or additional runtime dependencies merely to improve benchmark numbers without a demonstrated user-facing benefit.

## Transcript correctness contract

Readable transcript output must:

- follow the active `current_node` path only;
- preserve visible user/assistant turns in order;
- exclude abandoned regenerated branches;
- exclude visually hidden records;
- exclude tool-directed records;
- exclude analysis/reasoning/internal thought structures;
- exclude `reasoning_recap` timing records;
- preserve factual attachment-reference markers;
- never invent content for unsupported structures.

Compatibility diagnostics must surface schema/graph anomalies rather than silently treating unknown structures as ordinary conversation turns.

## Continuation validation priorities

For continuation exports, correctness takes precedence over speed. Preserve checks for:

- matching turn-start / turn-end marker counts;
- sequential turn IDs;
- speaker/heading agreement;
- user + assistant count parity;
- exactly one generated final continuation marker;
- historical endpoint metadata;
- attachment-manifest consistency;
- embedded instruction parity;
- manifest metadata integrity;
- per-payload SHA-256 verification;
- safe flat ZIP entry paths;
- no unexpected archive entries;
- historical transcript text that intentionally or accidentally resembles splitter framing syntax.

Verification should remain section/state aware rather than relying on whole-file regular-expression counts.

## Diagnostic privacy

Activity Log data remains in memory unless the user explicitly saves it.

**Save Redacted...** must remain safe even when an import fails after partial indexing. Sensitive-value tracking therefore must not depend solely on successfully populated UI rows. Any new Activity Log message that can contain a conversation title, stable conversation identifier, or local filesystem value must either register that value with the ActivitySink redaction registry or be emitted in a form that the redaction collector safely recognizes.

Clearing the Activity Log may clear historical redaction candidates only after rebuilding candidates required by the currently loaded source/rows.

## Release engineering

The recommended Windows build is the self-contained, managed-compressed portable package with the EXE plus the five required native WPF runtime libraries. The one-EXE build remains a comparison target only because it extracts native files into the .NET bundle cache at runtime.

Official release packages also include project and Microsoft/.NET legal-notice files. These documentation files are separate from the six-binary runtime-layout invariant.

CI must continue to assert:

- exact six-runtime-binary portable layout;
- reproducible deployed runtime binaries;
- zero application-owned TCP/UDP endpoints;
- ASLR, DEP/NX, and high-entropy VA;
- zero controlled `TEMP`, `TMP`, `APPDATA`, `LOCALAPPDATA`, and bundle-extraction residue;
- startup survival with a real main-window handle/title using the production managed-compressed configuration;
- deterministic SDK/toolchain selection.

The application must remain `asInvoker` and must not require administrator privileges.

Stable release packaging must only build an explicitly supplied immutable `vX.Y.Z` tag. The workflow must verify that the tag points exactly at the checked-out source and that the tag version equals the authoritative `<Version>` in `Directory.Build.props` before any stable package is accepted.

Do not promote a pull-request synthetic-merge artifact as the final release binary.

## Third-party runtime notices

Self-contained Windows releases redistribute Microsoft .NET/WPF runtime components. Preserve the release pipeline requirement that official packages include:

- the LLM Continuity Toolkit source-available license;
- `MICROSOFT-RUNTIME-NOTICES.txt`;
- the license supplied with the pinned Windows .NET installation;
- the third-party notices supplied with the pinned Windows .NET installation.

Do not replace third-party license terms with the project's source-available license.

## Windows UI release QA

The application uses native WPF controls and Per-Monitor V2 DPI awareness. Before a stable release, manually verify the final packaged build at representative Windows scaling levels:

- 100%;
- 125%;
- 150%;
- 200%.

For the main window and About/export-result dialogs, verify:

- text is not clipped or overlapped;
- buttons remain reachable;
- focus indication remains visible;
- normal Tab/Shift+Tab traversal is sensible;
- Space/Enter activate focused native controls as expected;
- Escape closes cancellable/modal dialogs where appropriate;
- maximize/restore respects the Windows taskbar working area;
- the UI remains usable at the declared minimum window size;
- moving the window between monitors with different DPI does not corrupt layout.

Before publishing a newly branded release, visually inspect the application icon and other graphic assets to confirm they do not imitate third-party product marks.

## Windows product metadata

`Directory.Build.props` is the single source of truth for release identity. Keep product, author/company, version/file version/assembly version, description, copyright, and repository metadata centralized there.

`AppInfo.Version` should continue to derive from assembly metadata so the main window, About dialog, generated manifests, and logs report one consistent product version.

## Tests

Repository tests must use synthetic data only. Never commit real ChatGPT export data, Activity Logs containing conversation names/content, or generated continuation archives derived from real data.

Maintain coverage for:

- branch reconstruction;
- visibility classification;
- attachment handling;
- continuation golden output;
- self-referential continuation framing;
- bundle corruption/tamper detection;
- cancellation and staging cleanup;
- malformed/cyclic graph handling;
- deterministic randomized graph hardening;
- Complete JSON preservation/fingerprints;
- one-pass batch raw export;
- direct-JSON and ZIP input-size safety;
- deployment layout/residue assertions;
- diagnostic redaction after partial/failed import paths.

The local parity-audit utility may be used against private files on a developer machine, but those files remain outside Git.

## Change discipline

Once a parser/export build has passed acceptance, avoid speculative changes to the parsing contract. Parser/schema changes should be motivated by one of:

1. a new official ChatGPT export structure;
2. a concrete regression;
3. a compatibility diagnostic showing an unsupported structure;
4. a security/correctness defect proven by a test case.

For UI or release-only changes, keep parser/export code untouched whenever possible.

After v2.3.0, the default posture is stable maintenance rather than continuing optimization work without evidence.
