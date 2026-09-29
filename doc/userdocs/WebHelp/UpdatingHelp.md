# Updating Purple Pen Help

This directory is the Purple Pen Help website: plain HTML pages with a Contents tree on the left
and a Search tab, served as a static website. There is no build step for the pages themselves.
Edit a file, reload it in the browser, and the change is there. The only generated part is the
search index, which `build-search.cmd` rebuilds.

The pages were converted once from the Windows HTML Help source in `../Help` by
`src/Tools/HelpConverter`. From now on, this directory is where help gets edited. Changes made
here don't flow back to `../Help`, and **don't re-run the converter**: it would overwrite
everything here (which is why it refuses to without `--force`).

## What the files are

| File | What it is | Edit it? |
|---|---|---|
| `*.htm` | The help topics, one per page. | Yes |
| `*.png`, `*.gif` | Screenshots and toolbar images used by the topics. | Yes (add or replace) |
| `toc.js` | The Contents tree on the left: titles, order and nesting. | Yes |
| `Stylesheet.css` | How topic text looks: the cyan heading band, fonts, margins. | Yes |
| `help.css` | How the layout looks: header, sidebar, tabs, tree, phone layout, printing. | Yes |
| `help.js` | Adds the header and sidebar to every page, draws the tree, runs the Search tab. The comment at the top explains how it works. | Rarely |
| `index.html` | Sends visitors to `Home.htm`, the front page. | Rarely |
| `build-search.cmd` | Rebuilds the search index. | No need |
| `pagefind/` | The search index and search box, **generated** by `build-search.cmd`. Not in git. | Never; it's overwritten on every rebuild |
| `UpdatingHelp.md` | This file. | Yes |

Two things to keep in mind with file names:

- **Don't rename existing topics.** Purple Pen opens help pages by file name
  (for example `ShowHelpTopic("Credits.htm")` in the app), and topics link to each other by file name.
- **Case matters on the web server.** The web server is probably Linux, where `Toolbar.gif` and
  `toolbar.GIF` are different files. Windows ignores the difference, so a link with the wrong case
  works on your machine and breaks on the web. Copy file names exactly. For new files, avoid
  spaces in the name too.

## Editing a topic

Open the `.htm` file in any text or HTML editor and change what's between `<main ...>` and
`</main>`. Leave the `<head>` alone: it's what gives the page its layout, Contents tree and search.

To see the change, reload the page in a browser. Opening the file directly (double-clicking it)
shows the page and the Contents tree, but **search only works when the pages come from a web
server**. See "Previewing locally" below.

After editing, run `build-search.cmd` before publishing, so that search finds the new text.

## Adding a new topic

1. **Create the page.** Copy an existing topic that is similar (a menu command, a dialog, an
   overview) and rename the copy, for example `EventNewThing.htm`. The existing names follow a
   `MenuCommand.htm` pattern: `FileSave.htm`, `EditAddControl.htm`, `ReportsCourseSummary.htm`.

2. **Keep the head, replace the content.** Every topic has this exact head. Only the `<title>`
   changes:

   ```html
   <!DOCTYPE html>
   <html lang="en">
   <head>
   <meta charset="utf-8">
   <meta name="viewport" content="width=device-width, initial-scale=1">
   <title>Event/New Thing</title>
   <link rel="stylesheet" href="Stylesheet.css">
   <link rel="stylesheet" href="help.css">
   <script src="toc.js" defer></script>
   <script src="help.js" defer></script>
   </head>
   <body>
   <main id="topic" data-pagefind-body>

   ...the topic goes here...

   </main>
   </body>
   </html>
   ```

   - `<title>` is what the browser tab shows.
   - The topic's first `<h1>` is what Search shows as the result title.
   - `data-pagefind-body` on `<main>` tells the search indexer that this is the text to index. A
     page without it is left out of search.

   A menu command topic usually starts with the cyan heading band, like this:

   ```html
   <div class="topicheading">
   <h1>Event/New Thing</h1>
   <p class="shortcut"><b>Toolbar Button:</b>
   <img src="ToolbarNewThing.gif" class="toolbar" align="absmiddle"></p>
   <p class="shortcut"><b>Keyboard Shortcut:</b> Ctrl+N</p>
   </div>
   <p>What the command does...</p>
   ```

   Leave out the toolbar and shortcut lines if the command has none.

3. **Add any images** to this directory and refer to them by file name: `<img src="NewThingDialog.png">`.

4. **Add it to the Contents tree** in `toc.js` (next section). A topic doesn't have to be in the tree.
   Some topics, like `Credits.htm`, are reached only by links from other topics or from the program.

5. **Link to it from other topics** where it helps: `<a href="EventNewThing.htm">new thing</a>`.

6. **Rebuild the search index** with `build-search.cmd`.

## Updating the Contents tree

The tree on the left of every page comes from `toc.js`. Each line is one entry:

```js
{ title: "Save", page: "FileSave.htm" },
```

- `title` is the text shown in the tree.
- `page` is the topic file it opens.

A folder has `children`, a list of the entries inside it:

```js
{ title: "File Menu", page: "FileMenu.htm", children: [
    { title: "New Event...", page: "FileNewEvent.htm" },
    { title: "Open...", page: "FileOpen.htm" },
    { title: "Save", page: "FileSave.htm" }
] },
```

A folder can have a page of its own (clicking its title opens `FileMenu.htm`) or not. Leave out
`page` and its title is plain text that just opens and closes the folder:

```js
{ title: "Create PDFs", children: [
    ...
] },
```

To add, remove or move a topic, add, delete or move its line. The tree shows entries in the order
they appear in the file. Folders nest as deeply as you like.

**Watch the commas.** Entries in a list are separated by commas: every entry but the last in its
list ends with `,`. A missing or extra comma, or a missing quote or bracket, stops the whole tree
from appearing. If the tree disappears after an edit, open the browser's developer tools (F12),
look at the Console tab, and it will name the line in `toc.js` with the mistake.

When a page is shown, the tree automatically opens the folders leading to it and highlights it.
Nothing else needs to change.

## Rebuilding the search index

Double-click `build-search.cmd`, or run it from a command prompt. It takes a second or two.

It deletes the `pagefind` directory and rebuilds it from every `.htm` page here. Run it after any
change to page text, and always before publishing: the Search tab only knows what the pages said
the last time it ran. (Changes to `toc.js`, images or the stylesheets don't need it.)

The first time it runs on a machine it downloads Pagefind, so it needs an internet connection then.

## Previewing locally

Search loads its index with web requests, which browsers don't allow for files opened straight
from disk. So to try search, serve this directory with a local web server. With Node.js installed
(see "Setting up a new machine" below), from a command prompt in this directory:

```
npx -y http-server -p 8080 -c-1
```

then browse to <http://localhost:8080/>. (`-c-1` turns off caching, so reloading always shows the
latest edits.) Stop it with Ctrl+C. If Python is installed, `python -m http.server 8080` works just
as well.

Stop the web server before running `build-search.cmd`. Otherwise it may keep a file in `pagefind`
open and Windows reports "being used by another process". The rebuild still works, but the
message is misleading.

## What to publish to the web server

Run `build-search.cmd` first, then copy these to the help's directory on the web server
(for example `https://purple-pen.org/help/`):

- every `*.htm` file
- `index.html`
- every image: `*.png`, `*.PNG`, `*.gif`
- `Stylesheet.css` and `help.css`
- `help.js` and `toc.js`
- the whole `pagefind` directory, with everything in it and its `fragment` and `index`
  subdirectories. Search needs all of it. Replace the old copy on the server rather than copying
  over it, so that the previous build's files don't pile up.

Don't publish `build-search.cmd` or `UpdatingHelp.md`. They're only for editing.

In other words, everything in this directory except those two files.

The help works in any directory on the server. Nothing in it assumes a particular address, so
it can move without changes. If it moves, though, update the address Purple Pen opens for help
(`WebsiteLauncherService.ShowHelpTopic` in `src/AvPurplePen`).

## Setting up a new machine

The pages need nothing: any text editor and browser will do. Only the search index (and the local
preview server) needs a tool, **Pagefind**, which is run through **Node.js**.

1. **Install Node.js**, the LTS (long-term support) version, from <https://nodejs.org/>, or from a
   command prompt:

   ```
   winget install OpenJS.NodeJS.LTS
   ```

   This also installs `npm` and `npx`, the Node.js package tools. Close and reopen any command
   prompts afterwards so they can find them.

2. **Check it worked**: `npx --version` in a new command prompt should print a version number.

That's all. There's nothing to install for Pagefind itself: `build-search.cmd` runs
`npx -y pagefind@1.5.2`, which downloads that version of Pagefind on first use, caches it, and
runs it. It needs an internet connection only that first time. No `package.json` and no
`node_modules` directory are involved.

The Pagefind version is fixed in `build-search.cmd` so that the search box doesn't change
underneath `help.js` unexpectedly. To move to a newer Pagefind, change the version number there,
rebuild, and check that the Search tab still works (search for something, click a result, and
check that the Search tab and its results are still showing on the new page).

Pagefind can also be installed without Node.js, as a Python package (`pip install "pagefind[extended]"`,
then run `python -m pagefind` instead of `npx -y pagefind@1.5.2`) or as a single program downloaded
from <https://github.com/Pagefind/pagefind/releases>. `build-search.cmd` would need that one line
changed.

### Also needed only to re-run the converter

`src/Tools/HelpConverter` needs the .NET 10 SDK. It was a one-time conversion, though, and
shouldn't need running again.
