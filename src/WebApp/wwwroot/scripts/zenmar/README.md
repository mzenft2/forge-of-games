# Zenmar Strategy engine

Auto layout engine used by the City Planner (`/city-planner/auto-layout`).

- `zenmar-engine.js` — generated bundle of the Zenmar Strategy engine (`engine.js`, `search-pool.js`,
  `patterns.js`) by Marek Zenft, https://github.com/mzenft2/zenmar-strategy, AGPL-3.0. Do not edit by
  hand; rebuild it with `build.mjs` from the engine repository. Under AGPL section 7(b) the authorship
  notice and the visible "Zenmar Strategy" attribution must be kept.
- `auto-layout-worker.js` — Web Worker that runs the engine off the UI thread and trims the result.
- Building data is not bundled: the page builds the engine catalog from Forge core data
  (`ZenmarCatalogFactory`) and passes it in `input.catalog`.
