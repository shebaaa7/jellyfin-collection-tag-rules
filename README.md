# Collection Tag Rules

Collection Tag Rules is a Jellyfin 12 plugin that runs a scheduled task to auto-tag items missing
a set of "marker" tags, so you can filter the dashboard down to everything that *isn't* some group
without hand-tagging every new item that gets added later.

## How it works

You define one or more rules on the plugin's dashboard config page. Each rule has:

- **Exclude tag** — the tag applied to items that have none of the marker tags.
- **Marker tags** — tags that mark an item as belonging to the group. An item with any one of
  these is left alone.
- **Item types** — which kinds of items the rule scans: Collections (Box Sets), TV Series, Movies,
  Music Albums, or Playlists.

On each run, the "Apply collection tag rules" scheduled task (Dashboard → Scheduled Tasks →
Library, daily at 3am by default, or Run Now) evaluates every enabled rule:

- No marker tag present, no exclude tag yet → the exclude tag is added.
- A marker tag is present but the exclude tag is still there from an earlier run → the exclude
  tag is removed. This makes rules self-correcting: retag an item into the group later and the
  plugin un-excludes it automatically on the next run, no manual cleanup needed.

### Example

To filter a Collections screen down to "everything not marked for kids", given collections already
tagged `Kids` or `Family`:

| Field | Value |
| --- | --- |
| Exclude tag | `Not Kids Content` |
| Marker tags | `Kids`, `Family` |
| Item types | Collections (Box Sets) |

After a run, filtering the Collections screen by tag `Not Kids Content` shows every collection that
isn't part of that group.

## Current features

- Multiple independent rules, each with its own exclude tag, marker tags, and item-type scope
- Curated, clearly-labeled item-type options rather than every internal item kind
- Self-correcting: the exclude tag is removed automatically once a marker tag is added
- Case-insensitive tag matching
- Dashboard config page with add/remove rule editing, no rebuild needed to add new rules

## Installation

Once a public release is available, the repository URL can be added under **Dashboard → Plugins →
Repositories**:

```text
https://raw.githubusercontent.com/shebaaa7/jellyfin-collection-tag-rules/master/manifest.json
```

Jellyfin will then list "Collection Tag Rules" under **Catalog** for a normal install/update
through the dashboard, no manual file copying needed.

No release has been published yet (see Development status below), so for now, install manually:

1. Build the plugin (see Build below), or take a prebuilt `Jellyfin.Plugin.TagRules.dll` from a
   release.
2. Create a versioned folder beneath Jellyfin's plugin directory. Default paths:

   ```text
   Windows: C:\ProgramData\Jellyfin\Server\plugins\Collection Tag Rules_<version>
   Linux:   /var/lib/jellyfin/plugins/Collection Tag Rules_<version>
   ```

3. Copy two files into that folder:
   - `Jellyfin.Plugin.TagRules.dll` — from `Jellyfin.Plugin.TagRules/bin/Release/net10.0/` after
     building, or from a `dotnet publish` output.
   - `meta.json` — not produced by the build, so it must be created separately (it's also not
     tracked in this repo, since `artifacts/` is gitignored — a local build output, not shipped in
     git). Use this template, updating `"version"` if you're building a different one:

     ```json
     {
       "category": "General",
       "changelog": "Initial release: rule-based collection tagging (marker tags / exclude tag / item types), scheduled task.",
       "description": "",
       "guid": "5cb39fd3-a820-4504-b235-13158ea91d4f",
       "name": "Collection Tag Rules",
       "overview": "Auto-tags collections (or other configured item types) missing every marker tag in a rule, so you can filter the dashboard by the rule's exclude tag.",
       "owner": "",
       "targetAbi": "12.0.0.0",
       "timestamp": "2026-09-11T00:00:00.0000000Z",
       "version": "1.0.0.0",
       "status": "Active",
       "autoUpdate": false,
       "assemblies": []
     }
     ```

   The folder should end up containing just those two files:

   ```text
   Collection Tag Rules_1.0.0.0\
     Jellyfin.Plugin.TagRules.dll
     meta.json
   ```

4. On Linux, make sure the files are owned by the `jellyfin` user:
   `chown -R jellyfin:jellyfin "/var/lib/jellyfin/plugins/Collection Tag Rules_<version>"`.
5. If replacing an older version, move its folder aside (e.g. rename with a `.disabled` suffix)
   rather than leaving both in place, so Jellyfin doesn't load two copies of the plugin.
6. Restart Jellyfin so it rescans the plugins directory.
7. Check the Jellyfin log for `Loaded plugin: "Collection Tag Rules" "<version>"` to confirm it
   loaded, and that there are no `PluginManager: Error deserializing` entries nearby.

**`meta.json` encoding matters.** It must be plain UTF-8 with **no byte-order mark (BOM)**.
Jellyfin's JSON parser rejects a BOM with `'0xEF' is an invalid start of a value`, and when that
happens it doesn't just skip the plugin — it deletes the folder and silently falls back to
whatever older version of the same plugin it can still find, which is easy to mistake for a
successful deploy. On Windows, avoid `Set-Content -Encoding utf8` (it adds a BOM); use
`[System.IO.File]::WriteAllText(path, json, (New-Object System.Text.UTF8Encoding($false)))` or a
plain text editor set to save without a BOM instead.

Once installed, configure it under **Dashboard → Plugins → Collection Tag Rules** (see How it
works above), then run "Apply collection tag rules" from **Dashboard → Scheduled Tasks → Library**
via Run Now, or wait for its daily trigger.

## Build

Requirements:

- .NET SDK 10
- Jellyfin 12.0.0 packages

```powershell
dotnet build Jellyfin.Plugin.TagRules/Jellyfin.Plugin.TagRules.csproj --configuration Release
dotnet test Jellyfin.Plugin.TagRules.Tests/Jellyfin.Plugin.TagRules.Tests.csproj --configuration Release
```

The plugin DLL is produced under `Jellyfin.Plugin.TagRules/bin/Release/net10.0/`.

## Development status

Version `1.0.0.0` has been built, unit tested, and live-tested end to end (both the tag-add and
the self-correcting tag-removal paths) against real Jellyfin 12.0.0 servers on both Windows and
Linux. It has not yet been published as a Jellyfin repository release.
