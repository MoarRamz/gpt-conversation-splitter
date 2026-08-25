# Development Notes

## Design constraints

GPT Conversation Splitter is intentionally a small, portable, local Windows utility.

Avoid adding persistent infrastructure unless there is a compelling functional requirement. In particular, do not introduce:

- conversation caches;
- databases;
- telemetry;
- cloud synchronization;
- background services;
- automatic update daemons;
- account/API integration;
- unnecessary third-party framework/runtime dependencies.

Prefer temporary computation over persistent storage when the tradeoff is reasonable.

## Current architecture

Version 2.2 uses:

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

## Release engineering

The recommended Windows build is the self-contained, managed-compressed portable package with the EXE plus the five required native WPF libraries. The one-EXE build remains a comparison target only because it extracts native files into the .NET bundle cache at runtime.

CI must continue to assert:

- exact six-file portable layout;
- reproducible deployed binaries;
- zero application-owned TCP/UDP endpoints;
- ASLR, DEP/NX, and high-entropy VA;
- zero controlled `TEMP`, `TMP`, `APPDATA`, `LOCALAPPDATA`, and bundle-extraction residue;
- startup survival with a real main-window handle/title;
- deterministic SDK/toolchain selection.

The application must remain `asInvoker` and must not require administrator privileges.

Final release artifacts must be rebuilt from stable `main` after the release PR is merged. Do not promote an earlier PR synthetic-merge artifact as the final release binary.

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
- deployment layout/residue assertions.

The local parity-audit utility may be used against private files on a developer machine, but those files remain outside Git.

## Change discipline

Once a parser/export build has passed acceptance, avoid speculative changes to the parsing contract. Parser/schema changes should be motivated by one of:

1. a new official ChatGPT export structure;
2. a concrete regression;
3. a compatibility diagnostic showing an unsupported structure;
4. a security/correctness defect proven by a test case.

For UI or release-only changes, keep parser/export code untouched whenever possible.

After v2.2.0, the default posture is stable maintenance rather than continuing optimization work without evidence.
