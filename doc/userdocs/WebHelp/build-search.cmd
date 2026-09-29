@echo off
rem build-search.cmd
rem
rem Rebuilds the search index for Purple Pen Help. Run this after editing any help page, before
rem publishing the help: the Search tab only finds what was in the pages when this last ran.
rem
rem It uses Pagefind (https://pagefind.app), a free search tool for static websites, which needs
rem Node.js installed (npx comes with it). Pagefind reads every page in this directory and writes the
rem index, plus the search box script and styles, into the "pagefind" subdirectory. Only the text
rem inside <main id="topic" data-pagefind-body> is indexed, so the sidebar doesn't pollute results.
rem
rem The "pagefind" directory is not kept in git (see .gitignore), because it is rebuilt from the
rem pages in under a second. Publish all of it along with the pages; search needs every file in it.
rem
rem The version is pinned so that the search box doesn't change underneath help.js unexpectedly.
rem --glob is needed because Pagefind only looks at *.html files by default, and the topics are *.htm.

rem Pagefind writes new files but never deletes old ones, and the index files are named after their
rem contents, so each rebuild would otherwise leave the previous build's files behind. Start clean.

cd /d "%~dp0"
if exist pagefind rmdir /s /q pagefind
npx -y pagefind@1.5.2 --site . --glob "*.{htm,html}"
