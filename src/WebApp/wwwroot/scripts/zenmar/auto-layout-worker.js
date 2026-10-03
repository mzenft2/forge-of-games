/*!
 * Auto layout worker for the Forge of Games City Planner.
 * Runs the Zenmar Strategy engine (zenmar-engine.js, same folder) — Copyright © 2026 Marek Zenft,
 * https://github.com/mzenft2/zenmar-strategy, AGPL-3.0.
 *
 * In:  {type:'run', input}  engine input (see zenmar-engine.js), input.catalog holds building data
 *      {type:'stop'}        finish early and return the best city found so far
 * Out: {type:'progress', done, total, best?}
 *      {type:'result', best}
 *      {type:'error', message}
 * Results are trimmed to positions and a few numbers, the full engine result is ~600 kB.
 */
importScripts('zenmar-engine.js');

function slim(result) {
    if (!result || !result.choices || result.choices.length === 0) {
        return null;
    }
    const engine = ZenmarEngine.engine;
    const best = [...result.choices].sort(engine.rankChoices)[0];
    const stats = best.stats || {};
    const equivalent = stats.equivalent || {};
    let complete = false;
    try {
        complete = !!engine.layoutQuality(best.list, best.options).complete;
    } catch (e) {
        complete = false;
    }
    return {
        buildings: best.list.map(b => ({id: b.id, x: b.x, y: b.y, w: b.w, h: b.h})),
        food: stats.food || 0,
        goods: stats.goods || 0,
        coins: stats.coins || 0,
        sparks: stats.sparks || 0,
        spareWorkers: stats.spare || 0,
        totalValue: equivalent.total || 0,
        usedTiles: stats.used || 0,
        armyDeficit: (stats.armyDeficit || 0) > 1e-5,
        isComplete: complete,
    };
}

onmessage = async ({data}) => {
    if (data && data.type === 'stop') {
        ZenmarEngine.stop();
        return;
    }
    const input = data && data.type === 'run' ? data.input : data;
    try {
        const result = await ZenmarEngine.run(input, (done, total, checkpoint) =>
            postMessage({type: 'progress', done, total, best: checkpoint ? slim(checkpoint) : null}));
        postMessage({type: 'result', best: slim(result)});
    } catch (error) {
        postMessage({type: 'error', message: error && error.message ? error.message : String(error)});
    }
};
