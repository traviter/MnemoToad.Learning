function(strategies) {
    var createdIds = [];

    function create(overrides) {
        var created = strategies.create(overrides);
        createdIds.push(created.response.id);
        return created;
    }

    function stageForCleanup(id) {
        createdIds.push(id);
    }

    function cleanup() {
        var remaining = [];
        for (var i = 0; i < createdIds.length; i++) {
            try {
                strategies.remove(createdIds[i]);
            } catch (e) {
                karate.log('fixture cleanup failed (suppressed):', e);
                remaining.push(createdIds[i]);
            }
        }
        createdIds = remaining;
    }

    return {
        create: create,
        stageForCleanup: stageForCleanup,
        cleanup: cleanup
    };
}
