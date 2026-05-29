# Updating PdfSharp

## How PdfSharp is configured in Purple Pen

Purple Pen uses a customized build of [PdfSharp](https://github.com/empira/PDFsharp) for PDF
export (the `Map_PDF` rendering backend). We maintain our own fork with a handful of fixes on top
of the public library:

- **Public upstream:** `https://github.com/empira/PDFsharp.git` (the `empira` project).
- **Our fork:** `https://github.com/petergolde/PDFsharp.git` — upstream plus our custom fixes
  (miter-limit realization, `NewFigure` subpath, blend-mode support, font collections).
- **Inside Purple Pen:** the fork's source is embedded as a **git subtree** at
  `src/MapModel/PDFsharp`. It is *not* a git submodule. (We switched from a submodule because
  submodules don't work well with git worktrees.)

Because the source lives directly in the Purple Pen repository as ordinary tracked files:

- Every clone and every `git worktree` gets the PdfSharp source automatically — there is **no**
  `git submodule init` / `git submodule update` step.
- `Map_PDF.csproj` and `Map_PDF.Tests.csproj` reference the project directly:
  `..\PDFsharp\src\foundation\src\PDFsharp\src\PdfSharp\PdfSharp.csproj`.
- Build output (`bin/`, `obj/`) under `src/MapModel/PDFsharp` is ignored via the fork's own
  nested `.gitignore`, which came along with the subtree.

### Two repositories, two roles

| Repository | Role |
|---|---|
| **PdfSharp fork** (`petergolde/PDFsharp`), cloned separately on your machine | The integration point. This is where you merge changes from the public empira library and resolve any conflicts against our fixes. It has both an `origin` remote (our fork) and an `upstream` remote (empira). |
| **Purple Pen** (this repo) | The consumer. It pulls the fork's content in via git subtree. It has a remote named `pdfsharp-fork` pointing at `https://github.com/petergolde/PDFsharp.git`. |

The clean rule of thumb: **the fork repo is where upstream merges happen; Purple Pen only pulls the
result.** (You *can* also push fixes the other direction — see the last section.)

> If you are setting up a fresh machine, clone the fork somewhere outside the Purple Pen tree,
> e.g. `git clone https://github.com/petergolde/PDFsharp.git` and add the upstream remote:
> `git remote add upstream https://github.com/empira/PDFsharp.git`.
> In Purple Pen, confirm the consumer remote exists: `git remote get-url pdfsharp-fork`
> (add it with `git remote add pdfsharp-fork https://github.com/petergolde/PDFsharp.git` if missing).

---

## 1. Update the PdfSharp fork from the empira (public) library

Do this in your **separate clone of the fork**, not in Purple Pen.

```bash
d:
cd d:\Repos\Main\PDFsharp-fork

git fetch upstream                 # get the latest public PdfSharp
git checkout master
git merge upstream/master          # merge public changes into our fork
# ... resolve any conflicts, keeping our custom fixes ...
git push origin master             # publish the updated fork
```

If there are conflicts, they will be in the files our fixes touched. Resolve them so that both the
upstream change and our fix are preserved, then `git add` the resolved files and complete the merge.

You can also merge from a specific upstream tag/release instead of `master`, e.g.
`git merge v6.2.5`.

---

## 2. Update Purple Pen from the PdfSharp fork

Once the fork has been updated and pushed (section 1), bring those changes into Purple Pen.

Do this in the **Purple Pen** repository, with a clean working tree:

```bash
d:
cd d:\Repos\Main\PurplePen

git fetch pdfsharp-fork
git subtree pull --prefix=src/MapModel/PDFsharp pdfsharp-fork master --squash
```

Notes:

- `--squash` keeps Purple Pen's history clean: each pull adds a single squashed commit of the
  upstream delta plus a merge commit, rather than importing all of PdfSharp's individual commits.
- `git subtree pull` creates the merge commit for you. If it opens an editor for the merge message,
  just save and close.
- **Always pull from the same remote (`pdfsharp-fork`)** so subtree can find the previous import's
  baseline commit (recorded as `git-subtree-split` in the squash commit message). Mixing sources
  (e.g. pulling directly from empira one time) can confuse the baseline tracking.
- After pulling, rebuild and run the PDF tests to confirm everything still works:

  ```bash
  dotnet build src/MapModel/Map_PDF/Map_PDF.csproj -c Release
  dotnet test  src/MapModel/Map_PDF.Tests/Map_PDF.Tests.csproj -c Release
  ```

---

## 3. Push changes made inside Purple Pen back to the fork

Sometimes it's convenient to edit PdfSharp directly inside Purple Pen (for example, while debugging
a PDF-rendering issue you can change `src/MapModel/PDFsharp/...` in place). To preserve those edits
in the fork so they survive future upstream merges, push them back.

1. Make and **commit** your changes to files under `src/MapModel/PDFsharp` in Purple Pen as normal.
2. Push just the subtree's contents to a branch on the fork:

   ```bash
   d:
   cd d:\Repos\Main\PurplePen
   git subtree push --prefix=src/MapModel/PDFsharp pdfsharp-fork pp-fix-branch
   ```

   This extracts the history of the subtree directory and pushes it to a branch named
   `pp-fix-branch` on the fork (`pdfsharp-fork`). (`git subtree push` can be slow on large repos
   because it re-walks history; that's expected.)
3. In the **fork** repo, merge that branch into `master` and push:

   ```bash
   d:
   cd d:\Repos\Main\PDFsharp-fork
   git fetch origin
   git checkout master
   git merge origin/pp-fix-branch
   git push origin master
   ```

After that, the fix lives in the fork (canonical home) and will be carried forward when you merge
future upstream releases.

> **Recommendation:** to avoid having two places to edit the same code, prefer making PdfSharp fixes
> in the fork repo (section 1's repo) as the canonical source, and only use `git subtree push` when
> a fix originated inside Purple Pen during debugging.

---

## Quick reference

| Goal | Where | Command |
|---|---|---|
| Pull public PdfSharp into our fork | fork repo | `git fetch upstream && git merge upstream/master && git push origin master` |
| Update Purple Pen from our fork | Purple Pen | `git fetch pdfsharp-fork && git subtree pull --prefix=src/MapModel/PDFsharp pdfsharp-fork master --squash` |
| Send a Purple-Pen-made fix back to the fork | Purple Pen, then fork | `git subtree push --prefix=src/MapModel/PDFsharp pdfsharp-fork <branch>` then merge `<branch>` into `master` in the fork |
