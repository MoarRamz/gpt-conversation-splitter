# Security and Privacy

LLM Continuity Toolkit is designed as a local-first Windows utility for processing official ChatGPT export archives.

## Security model

The application is intentionally constrained:

- no application network requests;
- no application telemetry or analytics;
- no API keys or account integration;
- no administrator privileges;
- no service, tray process, updater, or startup task;
- no persistent conversation cache or database;
- no automatic persistence of Activity Log data;
- no execution of content from imported archives;
- no extraction of the complete source ZIP to disk during normal import;
- no dynamic assembly/native-library loading from imported data;
- child-process launch restricted to the explicit **Open Folder** action, which opens Windows Explorer.

Operating-system or .NET runtime crash reporting, if enabled in Windows, is controlled by Windows rather than by LLM Continuity Toolkit.

The Windows application manifest uses `asInvoker` and does not request elevation.

CI scans application source for prohibited networking, Registry/service/startup persistence, script-shell execution, dynamic-load capabilities, and unexpected child-process launch sites. Release gates also verify ASLR, DEP/NX, and high-entropy 64-bit virtual-address support in the built PE image.

The built executable is launched during release validation and must own **zero TCP endpoints and zero UDP endpoints** while idle.

A separate startup-survival gate uses the production-equivalent managed-compressed publish configuration and requires the published process to reach input-idle, remain alive through an additional grace period, and expose a real LLM Continuity Toolkit main-window handle/title.

## Windows SmartScreen and unsigned builds

The project does not currently use a public Authenticode certificate. A freshly downloaded executable can therefore show Microsoft Defender SmartScreen with:

```text
Windows protected your PC
Publisher: Unknown publisher
```

That warning means Windows does not have a publicly trusted Authenticode publisher identity/reputation for the EXE. It is not, by itself, a malware detection result.

Do **not** disable SmartScreen globally to run this utility.

For a downloaded v2.3.0 release, verify the exact ZIP SHA-256 value published alongside that build before extracting or running it. For example:

```powershell
Get-FileHash '.\LLM_Continuity_Toolkit_v2.3.0_Windows_Portable.zip' -Algorithm SHA256
```

The reported hash must exactly match the release value supplied for that package. If it does not match, do not run the file.

After a verified download, Windows may attach Mark-of-the-Web metadata to the ZIP. For a file you have verified and intentionally trust, Windows Explorer **Properties → Unblock → Apply** can remove that internet-zone marker before extraction. This is preferable to disabling SmartScreen system-wide.

A self-signed certificate is not used merely to suppress the warning because it would require installing and maintaining a private trusted root on each machine and would not provide the same public publisher validation as a trusted Authenticode identity.

## Portable release behavior

The executable runtime layout consists of the application EXE and five native WPF runtime libraries. Official portable packages additionally carry project and Microsoft/.NET legal-notice files.

A true one-EXE .NET/WPF build was tested and rejected as the primary release because it extracts native runtime files into the .NET bundle cache at runtime.

The release gate tests the recommended portable application with controlled locations for:

- `TEMP`;
- `TMP`;
- `APPDATA`;
- `LOCALAPPDATA`;
- `DOTNET_BUNDLE_EXTRACT_BASE_DIR`.

After the idle application exits, those controlled roots must contain **zero application-created files**.

Deleting the portable application folder removes the distributed application files; the application is not designed to install additional runtime components elsewhere.

The release gate produces:

- a SHA-256 checksum for the complete downloadable ZIP;
- `SHA256SUMS.txt` containing SHA-256 values for every packaged file;
- build-provenance diagnostics recording source/toolchain/runtime/security-gate information and packaged-file hashes.

The portable runtime is published twice from the same source inputs. The six deployed runtime binaries must hash identically across both publishes before release packaging proceeds.

Stable packaging is version/tag guarded: the checked-out immutable release tag must match the authoritative version in `Directory.Build.props`, or packaging fails.

## Third-party runtime notices

Official self-contained Windows packages redistribute Microsoft .NET / WPF components. The release pipeline includes the authoritative .NET license and third-party-notice files supplied with the pinned Windows .NET installation, along with `MICROSOFT-RUNTIME-NOTICES.txt` identifying applicable Microsoft licensing sources.

Those Microsoft and third-party terms are separate from the project's source-available `LICENSE.txt`.

## Input handling

ChatGPT exports are treated as untrusted structured data.

Relevant defenses include:

- streaming JSON processing;
- direct JSON and ZIP entry-size safety limits;
- exact single-`conversations.json` discovery;
- duplicate stable-conversation-ID rejection;
- active-path graph validation;
- cycle/broken-parent diagnostics;
- mapping-node and visible-message safety limits;
- suspicious ZIP compression-ratio rejection;
- structural visibility classification;
- fail-closed handling of unknown active/visible content;
- staged/atomic export writes;
- cancellation cleanup;
- safe flat archive entry requirements for generated bundles;
- SHA-256 verification of continuation payloads;
- final ZIP re-open and verification before output finalization.

Regression tests use synthetic data and cover malformed/truncated ZIPs, duplicate conversation entries, zero-length `conversations.json`, suspicious compression ratios, duplicate conversation IDs, future unsupported visible content, raw-source mutation, reserved Windows device filenames, Unicode/long filenames, and output collisions.

Generated continuation bundles reject altered payloads, missing instructions, altered manifests, unexpected entries, and unsafe/nested archive paths in regression tests.

### Future ChatGPT content types

Known internal ChatGPT content is safely excluded. Known visible content is converted into readable transcript history.

If a future active-path message contains an unknown visible structured content type, the application records the compatibility issue and **blocks Markdown, HTML, text, and Continuation Markdown exports for that conversation**. It does not silently pretend the transcript is complete.

**Complete Conversation JSON remains available** because it preserves the original raw record losslessly rather than attempting to interpret unsupported content.

### Lazy transcript integrity

The application keeps only lightweight conversation metadata after import and reconstructs selected transcripts directly from the original source when readable output is requested.

For each indexed conversation, it retains a SHA-256 fingerprint of the canonical visible transcript. Hydration recomputes that fingerprint and refuses export if the visible transcript content no longer matches the imported index.

The metadata index also retains a SHA-256 fingerprint of the complete raw conversation record. Complete Conversation JSON recomputes that fingerprint immediately before export and refuses to write if the source record changed after import.

Neither fingerprint is written to a persistent cache or database; both exist only in memory for the current application session.

## Diagnostic privacy

The normal Activity Log remains memory-only unless the user explicitly selects **Save Log...**.

**Save Redacted...** preserves timings, message counts, compatibility counters, memory data, and integrity/verification results while pseudonymizing registered conversation titles, conversation identifiers, and local filesystem values. Redaction candidates are retained independently of successful UI import state so diagnostic data from a partially indexed import remains redacted even if that import later fails.

This is the preferred diagnostic format when sharing a log for troubleshooting. As with any diagnostic file, users should still review it before sharing externally.

## Repository data policy

Never commit any of the following:

- official ChatGPT export ZIP files;
- `conversations.json` from a real export;
- generated conversation transcripts derived from real data;
- generated continuation bundles derived from real data;
- Activity Logs containing conversation metadata/content;
- diagnostic fixtures derived from real conversation data.

Repository regression tests must use synthetic data only.

## Reporting a security issue

Please avoid posting sensitive export data in a public issue. Report security-sensitive defects through **GitHub Private Vulnerability Reporting** for this repository, or provide a minimal synthetic reproduction that does not contain real conversation data.

## Independence

LLM Continuity Toolkit is an independent project and is not affiliated with, sponsored by, or endorsed by OpenAI. ChatGPT and GPT are trademarks of OpenAI.
