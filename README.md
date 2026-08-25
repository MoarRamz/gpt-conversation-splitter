# GPT Conversation Splitter

**Developed by DevMoarRamz**

Local-first Windows utility for extracting individual conversations from an official ChatGPT data export and producing archival or continuation-ready files.

## Downloading and running the application

> [!IMPORTANT]
> **GitHub's Code → Download ZIP downloads the source-code repository. It is not the runnable Windows application.**
>
> To run GPT Conversation Splitter, download the compiled **Windows Portable** package from the project's GitHub Release, extract it, and launch **`GPT Conversation Splitter.exe`**. Keep the EXE and its five native WPF DLLs together in the same folder.

The source repository contains the C# solution, project files, synthetic tests, build tooling, security checks, documentation, and GitHub Actions workflows. It intentionally does not contain a prebuilt EXE in the repository tree.

## Current release

- **Version:** v2.2.0
- **Platform:** Windows 10/11 x64
- **Application:** C# / WPF on .NET 10
- **Deployment:** self-contained six-file portable Windows folder; no installed .NET runtime required

## Purpose

GPT Conversation Splitter processes official ChatGPT export data locally and can:

- separate individual conversations for archival use;
- reconstruct the active `current_node` conversation path;
- produce continuation-ready Markdown for carrying prior context into a new ChatGPT conversation;
- preserve complete original conversation records as JSON;
- package multiple selected conversations into verified ZIP bundles.

The recommended handoff format is **GPT Continuation Markdown**. Multi-conversation continuation exports include the individual payloads, `00 - READ ME FIRST - Continuation Instructions.txt`, and `bundle-manifest.json`.

## Privacy and resource philosophy

- Local processing only.
- No application network requests.
- No telemetry or API key.
- No persistent conversation cache or database.
- No background service, updater, tray process, or startup task.
- No administrator privileges required.
- Activity Log data is memory-only unless explicitly saved.
- **Save Redacted...** pseudonymizes conversation titles and local filesystem paths for safer troubleshooting.
- Real exports, transcripts, generated bundles, and private logs are excluded from the repository.

The application is intentionally portable and stateless. It does not remember recent files, export folders, preferences, or other user state between runs.

## Portable Windows package

The production package contains one managed-compressed EXE plus five native WPF runtime libraries:

```text
GPT Conversation Splitter/
├── GPT Conversation Splitter.exe
├── D3DCompiler_47_cor3.dll
├── PenImc_cor3.dll
├── PresentationNative_cor3.dll
├── vcruntime140_cor3.dll
└── wpfgfx_cor3.dll
```

Managed single-file compression reduces the deployed footprint while preserving the no-residue architecture. A true one-EXE WPF deployment is intentionally not used because .NET/WPF extracts native runtime files into the bundle cache.

The six-file portable layout is release-gated for zero application-created residue in controlled `TEMP`, `TMP`, `APPDATA`, `LOCALAPPDATA`, and .NET bundle-extraction roots.

## Architecture

```text
Official ChatGPT export ZIP / conversations.json
        ↓
stream conversations.json
        ↓
active current_node reconstruction
        ↓
structural visibility classification
        ↓
derive counts / endpoint metadata
        ↓
stream canonical raw-record SHA-256
visible-transcript SHA-256
        ↓
retain lightweight metadata-only index
        ↓
selected readable export
        ↓
direct one-pass hydration from original source
        ↓
metadata + transcript + raw-record parity verification
        ↓
transactional export / verify / finalize
        ↓
release hydrated transcript bodies

Complete Conversation JSON
        ↓
stream selected original record(s) from source
        ↓
verify canonical raw-record fingerprint
        ↓
write / verify / finalize payload(s)
```

There is no transcript cache, hydration staging database, background indexer, or persistent application state.

### Source-integrity proofs

For each indexed conversation, the application retains in memory:

- a SHA-256 fingerprint of the canonical visible transcript;
- an independent SHA-256 fingerprint of the complete canonical raw conversation record.

Both are produced incrementally. Readable hydration recalculates and verifies them before export. Complete Conversation JSON independently verifies the raw-record fingerprint immediately before output is accepted. Neither fingerprint is persisted as application state.

### Continuation handoff integrity

GPT Continuation Markdown includes deterministic turn framing, handoff metadata, continuation guidance, a historical attachment-reference manifest, and one final continuation endpoint.

Verification is section/state aware. Historical transcript text that resembles splitter turn markers, attachment-manifest lines, headings, or endpoint text is treated as historical content rather than generated framing. Single-file continuation exports are staged and only moved to their final destination after verification succeeds.

### Future-schema safety

Known internal ChatGPT content is excluded and known visible content is rendered normally. Unknown active/visible structured content fails closed for Markdown, HTML, text, and GPT Continuation export rather than silently producing incomplete history. Complete Conversation JSON remains available because it preserves the raw record without interpreting unsupported content.

Duplicate stable ChatGPT conversation IDs cause import to fail rather than allowing ambiguous hydration.

## Windows application

The desktop application includes:

- Per-Monitor V2 DPI awareness and Windows long-path awareness;
- centralized product/company/version/file-description metadata for Windows Properties;
- application icon metadata;
- native dark WPF presentation;
- keyboard-focusable native controls and accessibility/automation names on primary actions;
- polite accessibility notification for changing status text;
- an **About** dialog describing the application's purpose, privacy model, technology stack, and developer attribution;
- version text sourced from assembly product metadata.

## Security and release hardening

The release gates include:

- exact .NET SDK pin via `global.json`;
- immutable full-SHA GitHub Actions references;
- no persisted checkout credentials;
- Dependabot monitoring for GitHub Actions and NuGet;
- source-policy checks against networking, Registry/service/startup persistence, script shells, dynamic assembly/native-library loading, elevation, and unexpected child-process sites;
- only Windows Explorer **Open Folder** is allowlisted as a child-process action;
- runtime audit requiring **0 owned TCP and 0 owned UDP endpoints**;
- Windows PE checks requiring ASLR, DEP/NX, and high-entropy VA;
- controlled-root zero-residue runtime audit;
- two independent portable publishes whose deployed binaries must hash identically;
- per-file `SHA256SUMS.txt`, package SHA-256, and build provenance;
- startup-to-input-idle measurement and startup-survival validation.

See [`SECURITY.md`](SECURITY.md) for the trust model.

## Windows SmartScreen

The project does not currently use a public Authenticode certificate. Fresh downloads can therefore show **Windows protected your PC / Publisher: Unknown publisher**. This is an unsigned-publisher/reputation warning and is not, by itself, a malware detection result.

Do not disable SmartScreen globally. Verify the release ZIP SHA-256 published with the GitHub Release before running a downloaded build.

## Input and filesystem hardening

ChatGPT exports are treated as untrusted structured data. Synthetic regression/release coverage includes:

- exactly one `conversations.json` entry;
- duplicate conversation IDs;
- zero-length JSON;
- malformed/truncated ZIPs;
- suspicious compression ratios;
- excessive mapping nodes and oversized visible messages;
- broken/cyclic active paths;
- unknown future visible content;
- raw-source mutation after indexing;
- reserved Windows device names;
- illegal filename characters and trailing dots/spaces;
- Unicode and long titles;
- output collisions;
- atomic/staged output cleanup and cancellation;
- self-referential continuation syntax embedded inside historical transcript content.

## Current capabilities

- Direct official ChatGPT export ZIP or `conversations.json` import.
- Active `current_node` reconstruction.
- Structural hidden/tool/reasoning filtering.
- Metadata-only retained conversation index.
- Direct one-pass selected hydration.
- Search and virtualized multi-selection.
- Select visible / Clear visible / Clear all.
- Cancellation for import, hydration, and export.
- Memory-only Activity Log with pause/copy/clear/save/redacted-save.
- GPT Continuation Markdown, Markdown, HTML, plain text, and Complete Conversation JSON.
- Single-file export for one conversation; verified ZIP bundle for multiple conversations.
- Transactional single-file finalization and staged bundle finalization.
- Continuation structural/handoff verification.
- Attachment-reference manifests.
- Bundle manifest + SHA-256 payload verification.
- Count-aware continuation instructions.
- One-pass multi-conversation Complete JSON extraction.
- Per-Monitor V2 dark WPF interface.
- Native About dialog and accessibility metadata for primary controls.
- Operation-boundary memory cleanup telemetry.

## Validation policy

Behavioral changes are validated with synthetic regression suites and private real-export acceptance. Private acceptance data is never committed or documented with identifying conversation titles, local paths, or corpus-specific metadata.

The design intentionally avoids databases, background services, preloaders, parallel parsers, persistent caches, plugin systems, updaters, and additional export formats unless a demonstrated need justifies the complexity.

## Developer parity audit

A developer-only utility can compare a current export against `[INDEX]` entries in a known-good Activity Log without committing either file:

```text
dotnet run --project tools/GPTConversationSplitter.ParityAudit -- <export.zip> <reference-activity-log.txt>
```

## Repository workflow

`main` is the stable release line:

```text
engineering branch
        ↓
regression + hostile-input + security + runtime gates
        ↓
private real-export acceptance when behavior changes
        ↓
review
        ↓
merge to main
        ↓
rebuild release artifacts from stable main
        ↓
tag / release checkpoint
```

Release ZIP and deployed-file hashes are generated from the final stable `main` build rather than copied from an earlier release-candidate artifact.
