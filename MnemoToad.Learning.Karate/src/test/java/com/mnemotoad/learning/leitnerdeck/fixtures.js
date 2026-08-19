function() {
    var fixtureContainer = karate.read('classpath:com/mnemotoad/learning/common/fixture-container.js');

    return fixtureContainer({
        create: function(overrides) {
            return karate.call('classpath:com/mnemotoad/learning/leitnerdeck/create-leitnerdeck.feature', overrides || {});
        },
        remove: function(leitnerDeckId) {
            karate.call('classpath:com/mnemotoad/learning/leitnerdeck/delete-leitnerdeck.feature', { leitnerDeckId: leitnerDeckId });
        }
    });
}
