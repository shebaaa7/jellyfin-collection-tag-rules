# Jellyfin Collection Tag Rules — Project Instructions

## Project scope

- Standalone Jellyfin plugin: a scheduled task that applies admin-defined "tag rules" to the
  library. Each rule has an exclude tag and a set of marker tags; items with none of the marker
  tags get the exclude tag added, items that later gain a marker tag get it removed again.
- Originated from a one-off request to auto-tag "Not Doctor Who" collections, but is deliberately
  general: rules (marker tags, exclude tag, which item types to scan) are fully editable from the
  plugin's dashboard config page, no rebuild needed to reuse it for a different grouping.
- Keep the initial feature set focused on this rule-evaluation behavior. Do not add unrelated
  tag-management features (renaming, inventory, etc. belong to the separate `jellyfin-tag-manager`
  plugin).

## Repository boundaries

- Work only inside this `jellyfin-collection-tag-rules` repository unless explicitly told
  otherwise.
- The sibling `jellyfin-ignore-per-library` and `jellyfin-tag-manager` directories are separate
  projects and repositories. Never edit, build, clean, commit, or otherwise modify them as part of
  this plugin's work.

## Shared local environment

- Before environment-specific integration testing, deployment, or release work, read
  `../CLAUDE.md` for the shared Windows server, Linux test VM, and publishing reference.
- Treat `../CLAUDE.md` as private, local-only information. Never copy its credentials, network
  details, machine names, or personal data into this repository, logs, examples, commits, or
  user-facing output.
- Environment details are reference information, not standing authorization to deploy, restart
  servers, publish releases, or make external changes.

## Technical baseline

- Target Jellyfin 12.0.0 and .NET 10 unless the project files are deliberately updated.
- C# with nullable reference types enabled and warnings treated as errors.
- Item-type scanning is a curated, clearly-labeled set (`Configuration/ItemTypeOptions.cs`) rather
  than exposing every `BaseItemKind` — keep new item types clearly labeled if added.
- Core rule-matching logic (`ScheduledTasks/TagRuleEvaluator.cs`) is kept free of Jellyfin server
  types so it stays unit-testable without a running server.

## Quality and safety

- Add tests for new rule-matching behavior in `TagRuleEvaluatorTests.cs`.
- Run the relevant build and tests before considering implementation work complete.
- Never place credentials, tokens, private hostnames, IP addresses, or personal media-library
  details in tracked files, fixtures, logs, or examples.

## Documentation

- Keep the README, manifest, build metadata, and version numbers consistent.
- Use generic example tag names (not necessarily even Doctor Who) in public documentation/tests
  where a real example isn't needed.

## Current development state

- Initial scaffold: `Plugin.cs`, `Configuration/PluginConfiguration.cs`, `Configuration/TagRule.cs`,
  `Configuration/ItemTypeOptions.cs`, `Configuration/configPage.html` (dynamic add/remove rule
  editor), `ScheduledTasks/TagRuleEvaluator.cs` (pure logic), `ScheduledTasks/TagRuleMaintenanceTask.cs`
  (the `IScheduledTask`/`IConfigurableScheduledTask` implementation).
- Local `dotnet build -c Release` is clean (0 warnings/errors) and all 8 unit tests pass as of
  2026-09-11. Note the actual Jellyfin 12.0.0 SDK shapes differ from the original throwaway spec
  this plugin was based on: `IScheduledTask`/`IConfigurableScheduledTask` live in
  `MediaBrowser.Model.Tasks` with a single `ExecuteAsync(IProgress<double>, CancellationToken)`
  method (no separate `Execute`), `BaseItemKind` lives in `Jellyfin.Data.Enums`, and
  `TaskTriggerInfo.Type` takes a `TaskTriggerInfoType` enum (`DailyTrigger`, not a `TriggerDaily`
  constant). Confirmed via reflection against the installed `Jellyfin.Controller`/`Jellyfin.Model`
  12.0.0 NuGet packages, not guessed.
- **Deployed and live-tested on the Windows dev server 2026-09-11, with approval.** Version
  1.0.0.0 deployed to `Collection Tag Rules_1.0.0.0` (no prior version existed, nothing to back
  up/disable). `meta.json` built from `artifacts/publish-1.0.0.0/meta.json` (verified BOM-free).
  Jellyfin restarted; log confirmed `Loaded plugin: "Collection Tag Rules" "1.0.0.0"` and the
  "Apply collection tag rules" task registered with its daily 3am trigger, zero exceptions since.
  Configured the original Doctor Who rule via the plugin config REST API
  (`ExcludeTag: "Not Doctor Who"`, `MarkerTags: ["Doctor Who Season", "Doctor Who Story"]`,
  `ItemTypes: ["BoxSet"]`) and ran the task via Run Now. Result matched the documented production
  state exactly: 192 total collections scanned, 62 tagged `Not Doctor Who`, 0 removed — i.e.
  192 total minus the known 130 Doctor Who collections (24 Season + 106 Story) = 62. Spot-checked
  via the Items API: all 62 `Not Doctor Who` items confirmed, and sampled Doctor Who collections
  confirmed to carry only their own marker tag, no cross-contamination. No exceptions logged
  during or after the run. The exclude-tag-removal self-correction path (marker tag added later ->
  exclude tag removed) is covered by `TagRuleEvaluatorTests` rather than a second live mutation, to
  avoid further editing real production collection metadata beyond this one proof run.
- **Deployed and live-tested on the Linux test VM 2026-09-11, with approval.** Synced source via a
  tarball over SSH/SCP (no rsync available on this Windows host) to `~/jellyfin-plugin/`. `dotnet
  test` on the VM: 8/8 pass. `dotnet publish` succeeded; deployed DLL + the same BOM-free
  `meta.json` (verified again on the VM) to `Collection Tag Rules_1.0.0.0` under
  `/var/lib/jellyfin/plugins/`, `chown jellyfin:jellyfin`. `systemctl restart jellyfin`: journal
  confirmed `Loaded plugin: Collection Tag Rules 1.0.0.0` and the "Apply collection tag rules"
  daily trigger registered, zero exceptions.
  Live-tested end to end against the VM's real Jellyfin server, using its two existing test movies
  (`nested-video`, no tags; `Regular Show: The Movie`, tagged `friendship`/`time travel`) since the
  VM has no BoxSet collections: configured a rule (`ExcludeTag: "Test Not Friendship"`,
  `MarkerTags: ["friendship"]`, `ItemTypes: ["Movie"]`) via the config REST API, ran the task —
  journal logged `Tag rule 'Test Not Friendship': scanned 2 item(s), added to 1, removed from 0.`,
  and `nested-video` correctly got the exclude tag while the already-`friendship`-tagged movie was
  untouched. Then added `friendship` to `nested-video` via an item update and reran the task —
  journal logged `... added to 0, removed from 1.`, confirming the self-correcting removal path
  live (not just unit-tested), matching `TagRuleEvaluatorTests`. Cleaned up afterward: both movies'
  tags restored to their original state, plugin config reset to `{"Rules":[]}`. No exceptions
  logged at any point.
- Both platforms (Windows dev server, Linux VM) now confirmed working end to end, add and remove
  paths both proven live on at least one platform.
- Doctor Who example content (README, `configPage.html` help text/placeholder, test fixtures)
  replaced with a generic Kids/Family example before going public — real-content examples aren't
  in scope for this plugin the way real library names were for `jellyfin-ignore-per-library`, but
  genericized anyway per the same lesson.
- **Made public 2026-09-11, with approval.** No prior git repo existed for this project (unlike
  ignore-per-library, which had to rewrite history to scrub leaked secrets before its own public
  push) — this one was git-init'd fresh, so there was no history to audit or rewrite. Ran the same
  audit anyway before the first commit: grepped all tracked file contents for name/hostname/IP/
  credential/API-key patterns — clean (the Windows server's API key and other environment details
  live only in the gitignored root `../CLAUDE.md`, never in this repo). `gh repo create
  shebaaa7/jellyfin-collection-tag-rules --public`, pushed `master`. Verified via
  `gh repo view --json isPrivate` (`false`) and the GitHub contents API that only the intended
  files are present — no `bin/`, `obj/`, or `artifacts/`. Repository:
  https://github.com/shebaaa7/jellyfin-collection-tag-rules
- Not yet done: cutting an actual GitHub release (tagged version + zip asset) and publishing a
  real `manifest.json` pointing at it — the README's "Installation" section currently documents
  the manifest URL Jellyfin would use, but that file doesn't exist in the repo yet, so adding the
  repository URL in a Jellyfin dashboard today would 404. Do that as an explicit follow-up.
