// Chooses how each report grid (RadzenDataGrid with the "mobile-cards" class) is laid out, based on the
// table and the space it has rather than on the device:
//   table  - the table fits its panel at comfortable column widths: show a normal table.
//   scroll - it is a little too wide (at most 25%) for a wide panel (desktop): scroll sideways, with the
//            first column pinned.
//   cards  - otherwise: one card per row. Phones, tablets and split screens, and on desktops tables so wide
//            that scrolling would hide most of each row (e.g. Trade Matches' 13 columns).
// The styles for each layout live in css/app.css under data-layout. Re-evaluated when a grid is added,
// resized or its rows change (paging, filtering, recalculation).
(() => {
    "use strict";

    const GRID_SELECTOR = ".rz-data-grid.mobile-cards";
    // Below this grid width an over-wide table becomes cards; at or above it, it scrolls sideways.
    // 980px puts tablets (e.g. a Surface Pro in landscape, grids about 920-970px wide beside the sidebar) on
    // cards and laptops or desktops (grids about 990px and up at 1440px wide) on scrolling tables.
    const CARDS_BELOW_WIDTH = 980;
    // Scroll only tables at most this much wider than their panel; wider ones read better as cards.
    const MAX_SCROLL_RATIO = 1.25;

    const lastWidth = new WeakMap();
    const pending = new Set();
    let frameRequested = false;

    function chooseLayout(grid) {
        if (!grid.isConnected) return;
        const table = grid.querySelector(":scope > .rz-data-grid-data > .rz-grid-table");
        if (!table) return;
        const panel = table.parentElement;

        // Sizing pass: lay the table out at comfortable widths and see whether it fits. Reading offsetWidth
        // forces a synchronous layout, and the final layout is set before the browser paints.
        grid.setAttribute("data-layout", "measure");
        const needed = table.offsetWidth;
        const available = panel.clientWidth;
        let layout = "cards";
        if (needed <= available + 1) layout = "table";
        else if (grid.clientWidth >= CARDS_BELOW_WIDTH && needed <= available * MAX_SCROLL_RATIO) layout = "scroll";
        grid.setAttribute("data-layout", layout);
        lastWidth.set(grid, grid.clientWidth);
    }

    function schedule(grid) {
        pending.add(grid);
        if (frameRequested) return;
        frameRequested = true;
        requestAnimationFrame(() => {
            frameRequested = false;
            const grids = [...pending];
            pending.clear();
            grids.forEach(chooseLayout);
        });
    }

    // Only width changes matter; switching layout changes a grid's height, which must not re-trigger it.
    const resizeObserver = new ResizeObserver(entries => {
        for (const entry of entries) {
            const width = Math.round(entry.contentRect.width);
            if (lastWidth.get(entry.target) !== width) schedule(entry.target);
        }
    });

    const watched = new WeakSet();

    function watch(grid) {
        if (watched.has(grid)) return;
        watched.add(grid);
        resizeObserver.observe(grid);
        schedule(grid);
    }

    function gridsIn(node) {
        if (!(node instanceof Element)) return [];
        const found = node.matches(GRID_SELECTOR) ? [node] : [];
        return found.concat([...node.querySelectorAll(GRID_SELECTOR)]);
    }

    new MutationObserver(mutations => {
        for (const mutation of mutations) {
            mutation.addedNodes.forEach(node => gridsIn(node).forEach(watch));
            // Rows re-rendered (paging, filtering, new results): re-check the grid they belong to.
            const target = mutation.target instanceof Element ? mutation.target : mutation.target.parentElement;
            const grid = target?.closest(GRID_SELECTOR);
            if (grid) schedule(grid);
        }
    }).observe(document.documentElement, { childList: true, subtree: true, characterData: true });

    gridsIn(document.documentElement).forEach(watch);
})();
