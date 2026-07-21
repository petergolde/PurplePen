# Fixing GitVersion "object not found" build failures (large git pack offsets)

## Symptom

Every `msbuild` / `dotnet build` fails during the GitVersion step with an error like:

```
GitVersion.MsBuild.targets(9,9): error MSB3073: The command "...gitversion.exe ... " exited with code 1.
  ...
  LibGit2Sharp.NotFoundException: object not found - no match for id (64ba0a6f9734f763f012631ab6a46a542a99bcf8)
     at GitVersion.GitRepository.GetNumberOfUncommittedChangesInternal()
     ...
```

The build was working and then "just started" failing, with no obvious change to the code
or history.

## Root cause

The referenced object is **not** actually missing or corrupt:

- `git cat-file -t <id>` returns a valid type (e.g. `tree`).
- `git fsck --full` reports no missing/corrupt objects.
- The repository is a full (non-shallow) clone.

The real problem is a git **pack offset** limitation in old libgit2:

- This repo's object store can grow a single pack file **over 3 GB** (large binary test
  files under `src/TestFiles/`, plus the PdfSharp subtree history).
- When `git gc` / auto-gc repacks the store, a needed object can end up stored at a byte
  offset **beyond 2 GiB** inside a pack (confirmed via `git verify-pack -v`).
- **GitVersion.MsBuild 5.12.0 bundles LibGit2Sharp 0.27 / libgit2 1.3**, which
  mis-resolves objects at pack offsets ≥ 2 GiB. Native git 2.44 reads them fine (the
  pack `.idx` v2 has a 64-bit large-offset table); the old libgit2 does not, so it throws
  `object not found` while counting uncommitted changes.
- The same old libgit2 also has a **fragile multi-pack-index (MIDX) reader**: it errors
  out on a signature/version/unknown-chunk mismatch instead of degrading gracefully
  (graceful unknown-chunk handling only arrived in libgit2 1.7.0). When it can't parse a
  MIDX, it falls back to reading packs directly — straight into the >2 GiB offset problem.

### Why it starts "suddenly"

The large pack (and the >2 GiB offset) can exist for a long time without breaking the
build. What flips it from green to red is a routine **`git gc` / auto-gc** that repacks the
store and/or writes a `multi-pack-index`, rearranging object resolution so a needed object
now resolves via the >2 GiB path. GitVersion misses its version cache on every new commit,
so once the pack layout crosses the threshold, every subsequent build fails.

## Fix

Keep every pack under 1 GiB so no object is ever stored at an offset ≥ 2 GiB, and keep the
fragile MIDX path out of the picture entirely. This is a **local git-object-store repair** —
it does not modify any tracked files, commits, or history, so there is nothing to push.

Close Visual Studio and any running build first (Windows locks the `.pack` files),
ensure a few GB of free disk, then from the repo root:

```
git config --local pack.packSizeLimit 1g
git config --local core.multiPackIndex false
del .git\objects\pack\multi-pack-index
git repack -a -d --cruft --max-pack-size=1g --max-cruft-size=1g
git fsck --full
```

What each step does:

- `pack.packSizeLimit 1g` (persistent, local) — future `gc`/repack also split packs, so
  the >2 GiB pack does not silently come back.
- `core.multiPackIndex false` — git will neither write nor read a multi-pack-index for this
  repo, keeping the fragile old-libgit2 MIDX path out of use.
- `del ...\multi-pack-index` — removes the existing MIDX.
- `git repack -a -d --cruft --max-pack-size=1g --max-cruft-size=1g` — rewrites all objects
  into packs each ≤ ~1 GiB (reachable objects) and small cruft packs (unreachable objects).
  Plain `git repack` without `--write-midx` does not create a MIDX.
- `git fsck --full` — verifies integrity. "**dangling**" tree/blob reports are normal —
  they are unreachable objects now living in the cruft pack, not corruption.

Caveat: `--max-pack-size` is a *soft* cap; a single object larger than the limit cannot be
split. Not a concern here — the largest objects are test images (jpg/png/gif), all well
under 1 GB.

## Verifying the fix

- `ls .git/objects/pack/` shows multiple packs each under ~1 GB, plus one `.mtimes` cruft
  pack, and **no `multi-pack-index`**.
- (Optional) `git verify-pack -v <pack>.idx | grep <id>` shows the object now at a small
  offset (was ~3.07 GB; after the repack it was ~51 MB).
- Run GitVersion 5.12 directly to reproduce the original command; it should now succeed
  (exit 0) and emit a version, e.g.:
  ```
  <nuget-cache>\gitversion.msbuild\5.12.0\tools\net48\gitversion.exe <some-project-dir> -output json
  ```
- A normal `msbuild PPen.slnx /p:Configuration=Release` completes past the GitVersion step
  and writes `obj\gitversion.json`.

## Important notes

- The two `git config --local` settings live in `.git/config`, which is **not tracked**.
  A **fresh clone** (or a reset config) will not have them, so if the failure reappears on
  another machine or clone, re-apply the fix there. Other clones / CI that hit the same
  error each need their own repack.
- **Longer-term alternative:** upgrade `GitVersion.MsBuild` from 5.12.0 to a current 6.x
  release, whose newer libgit2 handles >2 GiB pack offsets and MIDX files — after which the
  pack-size workaround is no longer needed. Trade-off: GitVersion 6 changed some
  version-calculation defaults, so verify the produced version numbers before committing to
  the upgrade.

*(First diagnosed and applied 2026-07-20.)*
