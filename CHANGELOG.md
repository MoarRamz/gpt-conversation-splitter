# Changelog

## 2.2.1 - 2026-08-25

### Public-readiness and privacy

- Established a sanitized public source baseline with no ancestry from the private engineering repository.
- Removed private acceptance-corpus identifiers, measurements, local-path references, and nonessential personal biography from public-facing project material.
- Kept application source, synthetic tests, security tooling, architecture documentation, and developer attribution required to understand and verify the program.
- Updated Windows product metadata and About presentation for the public distribution baseline.
- Adjusted public GitHub Actions workflows so validation remains available without publishing separate diagnostics artifacts.
- Kept stable release packaging manual-only so public `main` changes cannot publish a release artifact automatically.

### Behavior

- No parser, transcript-reconstruction, export-format, bundle-integrity, or persistence behavior changes.
- v2.2.0 remains the functional architecture baseline; v2.2.1 is the public-readiness patch release built from the sanitized public repository.

## 2.2.0 - 2026-08-25

### Streaming raw-record and export path

- Replaced full-record `SerializeToUtf8Bytes` SHA-256 work with canonical streaming JSON hashing through a low-overhead incremental hash sink.
- Reused the same incremental hashing approach for visible-transcript fingerprints.
- Complete Conversation JSON now streams top-level `conversations.json` arrays instead of parsing the entire document before export; wrapper/single-object compatibility retains a fallback path.
- Multi-conversation Complete JSON export scans the original source once, extracts selected records, verifies canonical raw-record fingerprints, and packages them.
- Retained `ChatExportReader` as the single parser/visibility oracle and removed an abandoned duplicate metadata-reader experiment.

### Continuation verification and transactional finalization

- Scoped historical attachment-reference verification to the actual attachment manifest rather than matching manifest-like lines across the entire generated Markdown file.
- Made continuation verification section/state aware so historical transcript content that resembles splitter turn markers, headings, attachment-manifest syntax, or the final continuation endpoint is not mistaken for generated framing.
- Added a dedicated self-referential continuation regression using synthetic transcript content.
- Single-file exports are written to a staging path and moved to the chosen final filename only after verification and cancellation checks succeed.
- Preserved active-`current_node` semantics and existing readable-output behavior.

### Bundle and diagnostic polish

- Complete JSON bundle verification remains manifest/hash based while the Activity Log reports continuation instructions as **not applicable** when that bundle format intentionally contains no instructions file.
- Final ZIP archives are re-opened, validated against the embedded manifest/instructions contract, checked for unsafe/nested entries, and re-hashed before being accepted.
- Redacted Activity Logs preserve performance, compatibility, memory, counts, and verification diagnostics while pseudonymizing conversation titles and local filesystem paths.

### Portable deployment

- Accepted managed single-file compression for the portable application after controlled comparison testing.
- Reduced the deployed six-file package footprint by approximately half while retaining the same no-residue deployment model.
- Kept the zero-runtime-extraction six-file layout: one managed-compressed EXE plus five native WPF runtime libraries.
- CI requires reproducible compressed binaries, ASLR/DEP/high-entropy VA, zero owned TCP/UDP endpoints, and zero controlled runtime/bundle-extraction residue.

### Finished Windows application polish

- Added a native **About GPT Conversation Splitter** modal describing the program's purpose, privacy-by-design model, technology stack, and developer attribution.
- About/version presentation reads the application version from assembly metadata used by exports and manifests.
- Added accessibility/automation names to primary actions, search, export-format selection, conversation grid, selection helpers, Activity Log controls, progress, and the About dialog.
- Marked changing status text as a polite accessibility live region.
- Enabled layout rounding/device-pixel snapping while preserving Per-Monitor V2 DPI awareness.
- Preserved centralized Windows file metadata in `Directory.Build.props`.

### CI and release engineering

- Promoted the dedicated v2.2 optimization workflow to RC2 packaging and added concurrency cancellation for superseded PR runs.
- Aligned the general Windows build workflow with the same managed-compressed six-file deployment contract.
- Kept separate startup-survival, full Windows build/release, and v2.2 optimization gates.
- Final release artifacts and hashes are rebuilt from stable `main` after merge rather than reusing a pull-request synthetic-merge artifact.

### Acceptance

- Full private real-export acceptance passed after the continuation-verifier hardening and final application polish.
- Synthetic regression, hostile-input, bundle-integrity, raw-record fingerprint, cancellation, filename-safety, startup, security, reproducibility, and zero-residue gates passed for the released architecture.
- Private acceptance data, conversation titles, local paths, and corpus-specific metadata are intentionally omitted from the public repository.

## 2.1.0 - 2026-08-23

### Memory and export architecture

- Replaced always-resident transcript bodies with a lightweight metadata-only in-session conversation index.
- Readable Markdown, HTML, Plain Text, and GPT Continuation exports directly rehydrate selected conversations from the original source in one scan.
- Removed the temporary hydration-JSON prototype; direct selected hydration creates no transcript cache or intermediate hydration file.
- Complete Conversation JSON remains on its direct raw-record path and does not perform unnecessary transcript hydration.
- Removed an unnecessary duplicate normalized-message collection on already-clean structured exports.
- Preserved the accepted parser/visibility semantics as the behavioral contract.

### Source integrity

- Added an in-memory SHA-256 fingerprint for each canonical visible transcript during metadata indexing.
- Added an independent SHA-256 fingerprint for each complete parsed raw conversation record.
- Selected readable hydration verifies metadata, the exact visible-transcript fingerprint, and the raw-record fingerprint before output is allowed.
- Complete Conversation JSON verifies the raw-record fingerprint immediately before export.
- A source that changes after indexing is rejected even when counts and endpoint metadata remain unchanged.
- Neither fingerprint is persisted as a cache or database.

### Future-schema and ambiguity safety

- Added fail-closed handling for unknown active/visible ChatGPT structured content: readable/continuation exports are blocked rather than silently omitting unsupported history.
- Complete Conversation JSON remains available for unsupported future content because it preserves the original raw record losslessly.
- Added hard rejection of duplicate stable conversation IDs so hydration never guesses between ambiguous records.
- Added visible-message size and suspicious ZIP compression-ratio safety limits.

### Hostile-input and filesystem hardening

- Added regression coverage for duplicate `conversations.json` entries, zero-length JSON, malformed/truncated ZIPs, suspicious compression ratios, duplicate conversation IDs, future unsupported content, and raw-source mutation.
- Hardened Windows output filenames against reserved device names.
- Added coverage for illegal characters, trailing spaces/dots, Unicode titles, long titles, and output collisions.
- Existing atomic output and cancellation cleanup behavior remains enforced.

### Diagnostic privacy

- Added **Save Redacted...** alongside the existing full **Save Log...** option.
- Redacted logs retain performance, memory, counts, compatibility, and integrity/verification diagnostics while pseudonymizing conversation titles and local filesystem paths.
- Both log formats remain opt-in; Activity Log data remains memory-only by default.

### Security and supply-chain hardening

- Pinned the build toolchain to an exact .NET SDK via `global.json` and disabled SDK roll-forward.
- Pinned GitHub Actions to immutable full commit SHAs and disabled persisted checkout credentials.
- Added Dependabot monitoring for GitHub Actions and NuGet.
- Expanded the CI architecture guard to reject networking, Registry/service/startup persistence, script-shell execution, dynamic assembly/native-library loading, elevation requests, and unexpected child-process launch sites.
- Restricted child-process launch to the explicit Windows Explorer **Open Folder** action.
- Release gates assert ASLR, DEP/NX, and high-entropy 64-bit address randomization.
- Added a runtime audit requiring the built EXE to own **0 TCP and 0 UDP endpoints** while idle.
- Expanded zero-residue testing to controlled `TEMP`, `TMP`, `APPDATA`, `LOCALAPPDATA`, and .NET bundle-extraction roots.
- Added reproducible-build verification for all deployed files.
- Added per-file `SHA256SUMS.txt`, package SHA-256, and build provenance.
- Added SmartScreen/unsigned-publisher guidance without recommending disabling SmartScreen.

### Startup reliability

- Added startup-to-input-idle measurement to the Windows CI gate.
- Corrected an RC startup crash in the redacted-log path detector.
- Added a separate startup-survival workflow requiring the published process to remain alive after input-idle with a real application window.
- Windows Properties metadata exposes clean semantic version values without a source-revision suffix.

### Acceptance

- Private real-export parity and smoke testing passed for the final v2.1 architecture.
- Public documentation intentionally omits private corpus measurements and conversation-specific identifiers.

## 2.0.0 - 2026-08-23

### Architecture

- Rewritten as a compiled C# / WPF application on .NET 10.
- Removed the PowerShell/VBS runtime dependency from the application architecture.
- Added a streaming `conversations.json` import pipeline.
- Added explicit Windows `asInvoker`, Per-Monitor V2 DPI-awareness, and long-path manifest declarations.
- Centralized product/version/developer metadata under `DevMoarRamz`.
- Final Windows distribution uses a six-file self-contained portable layout that creates no .NET bundle-cache files at runtime.

### Transcript correctness

- Reconstructs only the active `current_node` path.
- Structurally excludes hidden, tool-directed, analysis, reasoning, and `reasoning_recap` records.
- Preserves visible attachment-reference markers.
- Keeps a narrow defensive fallback for older exports where timing recaps were flattened into visible text.
- Added compatibility counters for excluded/internal records and graph/schema anomalies.

### Performance and memory

- Added streaming import and operation-boundary memory compaction with before/after Activity Log telemetry.

### Continuation bundles

- One selected conversation exports as a normal single file; multiple selected conversations package into one ZIP archive.
- GPT Continuation bundles include instructions and `bundle-manifest.json`.
- Bundle manifests include application/developer metadata and per-payload SHA-256 hashes.
- Final bundle verification reopens the ZIP, validates manifest/instructions, rejects unexpected/nested entries, and re-hashes every payload.

### Export formats and UI

- GPT Continuation Markdown, standard Markdown, HTML, plain text, and Complete Conversation JSON.
- Dark WPF interface with search, selection helpers, Activity Log controls, cancellation, and developer attribution.

### Verification and resilience

- Golden continuation-output assertions.
- Deterministic randomized malformed-graph testing.
- Reasoning-recap/timing regression fixtures.
- Archive corruption tests and cancellation cleanup tests.
- Developer-only parity-audit utility.
- Windows CI deployment-layout/runtime extraction diagnostics.

## 1.6.0 - Reference implementation

- Major PowerShell/WPF indexing and memory optimization milestone.
- Established the behavioral reference for the compiled v2 rewrite.
- Added structural + handoff continuation verification and performance telemetry.

## 1.4.4 - 2026-08-22

### Git baseline

- Established as the first Git-tracked stable checkpoint.
- Earlier development versions are intentionally not backfilled into Git history.
