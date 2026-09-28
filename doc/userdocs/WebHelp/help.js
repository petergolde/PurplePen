/*
 * help.js
 *
 * Builds the help-window layout around each Purple Pen Help page.
 *
 * HOW THE PAGES FIT TOGETHER
 *
 * Every topic (*.htm) is an ordinary HTML page. Its content is inside <main id="topic">, and its
 * <head> loads four shared files:
 *
 *   Stylesheet.css  How topic text looks (the cyan heading band, margins, and so on).
 *   help.css        How the layout below looks.
 *   toc.js          The Contents tree, as data (window.helpToc). Edit it to add or move topics.
 *   help.js         This file. It adds a header and a sidebar around <main>, so every topic
 *                   gets the same layout without each page having to contain it:
 *
 *       +------------------------------------------------------------+
 *       | <header>   [menu button]  Purple Pen Help                  |
 *       +--------------------+---------------------------------------+
 *       | <nav id="sidebar"> | <main id="topic">                     |
 *       |  [Contents|Search] |                                       |
 *       |                    |   the topic, exactly as written       |
 *       |  > Getting Started |   in the .htm file                    |
 *       |  v User Interface  |                                       |
 *       |      Selecting     |                                       |
 *       +--------------------+---------------------------------------+
 *
 * help.css arranges <header>, <nav> and <main> into that grid. The menu button only appears on
 * narrow screens (phones), where the sidebar is hidden until the button is pressed.
 *
 * THE CONTENTS TREE
 *
 * The tree is ordinary nested lists (<ul>/<li>). An entry with sub-entries is a <details> element,
 * whose <summary> is the line you click to open or close it. The browser opens and closes
 * <details> by itself, so there is no JavaScript here for expanding or collapsing. This file only
 * builds the lists from toc.js, then opens the folders leading to the page being shown and
 * highlights it.
 *
 * SEARCH
 *
 * The Search tab uses Pagefind (https://pagefind.app). build-search.cmd reads every page and writes a
 * search index plus Pagefind's ready-made search box (pagefind/pagefind-ui.js and .css). This file
 * loads that search box the first time the Search tab is opened. So that clicking one search result
 * after another works like the old Windows help, the chosen tab and the search words are remembered
 * while moving from page to page (in sessionStorage, which lasts until the browser tab is closed).
 *
 * TO ADD A NEW TOPIC
 *
 *   1. Copy an existing .htm file, keeping its <head>, and replace what is inside <main>.
 *   2. Add a line for it in toc.js.
 *   3. Run build-search.cmd so that search finds it.
 */

// Everything is inside this function so that its names can't clash with anything else on the page.
(function () {
    "use strict";

    // The names under which the chosen tab and search words are remembered between pages.
    const TAB_SETTING = "purplePenHelp.tab";
    const SEARCH_SETTING = "purplePenHelp.search";

    // Whether startSearch has already loaded the search box on this page. (It has to be declared
    // up here, before the steps below run, because a "let" can't be used before its line.)
    let searchStarted = false;

    // help.js is loaded with "defer", so by the time it runs the whole page has been read and
    // <main> exists. These are the steps, in order; each is a function below.
    buildLayout();
    showCurrentTopic();
    setUpTabs();
    setUpMenuButton();


    /*
     * Adds the header and sidebar to the page, in front of <main>. The markup is written out in
     * full here, so this is the place to change what the header and sidebar contain.
     */
    function buildLayout() {
        const layout = `
            <header id="help-header">
                <button type="button" id="menu-button" aria-label="Show or hide the contents">&#9776;</button>
                <a id="help-title" href="Home.htm">Purple Pen Help</a>
            </header>
            <nav id="sidebar">
                <div id="tabs">
                    <button type="button" id="contents-tab" class="tab selected">Contents</button>
                    <button type="button" id="search-tab" class="tab">Search</button>
                </div>
                <div id="contents-panel" class="panel">
                    <ul class="toc">${renderTree(window.helpToc || [])}</ul>
                </div>
                <div id="search-panel" class="panel" hidden></div>
            </nav>`;

        document.body.insertAdjacentHTML("afterbegin", layout);
    }


    /*
     * Returns the HTML for a list of Contents entries (from toc.js), as <li> elements. Calls itself
     * for the entries inside each folder, so the whole tree is built from one call.
     *
     * A plain topic becomes:     <li class="topic"><a href="Page.htm">Title</a></li>
     * A folder becomes:          <li class="folder"><details>
     *                                <summary><a href="Page.htm">Title</a></summary>
     *                                <ul> ...its entries... </ul>
     *                            </details></li>
     * A folder with no page shows its title as plain text instead of a link.
     */
    function renderTree(entries) {
        return entries.map(function (entry) {
            const label = entry.page
                ? `<a href="${escapeHtml(entry.page)}">${escapeHtml(entry.title)}</a>`
                : `<span>${escapeHtml(entry.title)}</span>`;

            if (entry.children) {
                return `<li class="folder"><details><summary>${label}</summary>
                            <ul>${renderTree(entry.children)}</ul>
                        </details></li>`;
            }
            else {
                return `<li class="topic">${label}</li>`;
            }
        }).join("");
    }


    /*
     * Highlights the current page in the Contents tree, opens the folders that contain it, and
     * scrolls the tree so that it can be seen. (Some topics are only reached from links in other
     * topics and aren't in the tree; then nothing is highlighted.)
     */
    function showCurrentTopic() {
        for (const link of document.querySelectorAll("#contents-panel a")) {
            // link.pathname is the link's full path, like "/help/EditAddControl.htm", so it can be
            // compared directly with the path of the page being shown.
            if (link.pathname === location.pathname) {
                link.classList.add("current");

                // Open the <details> the link is in, then the one around that, and so on up.
                let folder = link.closest("details");
                while (folder) {
                    folder.open = true;
                    folder = folder.parentElement.closest("details");
                }

                link.scrollIntoView({ block: "center" });
                return;
            }
        }
    }


    /*
     * Makes the Contents and Search tabs switch the sidebar between the tree and the search box.
     * If the Search tab was showing on the previous page (say, after clicking a search result),
     * shows it again.
     */
    function setUpTabs() {
        document.getElementById("contents-tab").addEventListener("click", function () { showTab("contents"); });
        document.getElementById("search-tab").addEventListener("click", function () { showTab("search"); });

        if (readSetting(TAB_SETTING) === "search") {
            showTab("search");
        }
    }


    /*
     * Shows one sidebar tab and hides the other. tabName is "contents" or "search".
     */
    function showTab(tabName) {
        const showingSearch = (tabName === "search");

        document.getElementById("contents-tab").classList.toggle("selected", !showingSearch);
        document.getElementById("search-tab").classList.toggle("selected", showingSearch);
        document.getElementById("contents-panel").hidden = showingSearch;
        document.getElementById("search-panel").hidden = !showingSearch;

        saveSetting(TAB_SETTING, tabName);
        if (showingSearch) {
            startSearch();
        }
    }


    /*
     * Loads Pagefind's search box into the Search tab, the first time the tab is shown on this
     * page, and repeats the search from the previous page, if there was one.
     */
    function startSearch() {
        if (searchStarted) {
            return;
        }
        searchStarted = true;

        const searchPanel = document.getElementById("search-panel");

        addStylesheet("pagefind/pagefind-ui.css");
        addScript("pagefind/pagefind-ui.js", function () {
            // PagefindUI is defined by pagefind-ui.js. Its options are described at
            // https://pagefind.app/docs/ui/
            const searchBox = new PagefindUI({
                element: "#search-panel",
                showImages: false,
                excerptLength: 20,
                autofocus: true,

                // How many results to show at first, and how many more each "Load more results"
                // click adds.
                pageSize: 10,

                // Search results link to pages by their path within the help, like "/Home.htm".
                // Putting this page's directory in front makes the links work wherever the help
                // is published, such as https://purple-pen.org/help/.
                baseUrl: new URL(".", location.href).pathname
            });

            const previousSearch = readSetting(SEARCH_SETTING);
            if (previousSearch) {
                searchBox.triggerSearch(previousSearch);
            }

            // Remember the search words when leaving this page, so the next page can repeat the search.
            window.addEventListener("pagehide", function () {
                const input = searchPanel.querySelector("input");
                saveSetting(SEARCH_SETTING, input ? input.value : "");
            });
        }, function () {
            // pagefind-ui.js is missing: build-search.cmd hasn't been run, or the "pagefind"
            // directory wasn't published along with the pages.
            searchPanel.innerHTML = `<p class="search-unavailable">Search is not available.</p>`;
        });
    }


    /*
     * On narrow screens, where the sidebar is hidden, makes the menu button in the header show and
     * hide it. (help.css shows the sidebar when <body> has the class "sidebar-open".)
     */
    function setUpMenuButton() {
        document.getElementById("menu-button").addEventListener("click", function () {
            document.body.classList.toggle("sidebar-open");
            showCurrentTopic();   // The tree couldn't be scrolled while hidden, so do it now.
        });
    }


    // ---- Small helpers ----

    /*
     * Turns text into HTML that displays that text, so that a title such as "Lines & Areas" doesn't
     * get mistaken for markup.
     */
    function escapeHtml(text) {
        return text.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
    }

    /*
     * Adds a stylesheet to the page.
     */
    function addStylesheet(url) {
        document.head.insertAdjacentHTML("beforeend", `<link rel="stylesheet" href="${url}">`);
    }

    /*
     * Adds a script to the page, and calls onLoad once it has run, or onError if it can't be loaded.
     * (A <script> inserted as HTML text never runs, which is why this one is created differently.)
     */
    function addScript(url, onLoad, onError) {
        const script = document.createElement("script");
        script.src = url;
        script.onload = onLoad;
        script.onerror = onError;
        document.head.appendChild(script);
    }

    /*
     * Reads a value remembered with saveSetting, or returns null if there isn't one. Browsers can
     * refuse access to sessionStorage (in some private-browsing modes, for example); then nothing is
     * remembered, and the help still works.
     */
    function readSetting(name) {
        try {
            return sessionStorage.getItem(name);
        }
        catch (e) {
            return null;
        }
    }

    /*
     * Remembers a value until the browser tab is closed.
     */
    function saveSetting(name, value) {
        try {
            sessionStorage.setItem(name, value);
        }
        catch (e) {
            // Not remembered; see readSetting.
        }
    }
})();
