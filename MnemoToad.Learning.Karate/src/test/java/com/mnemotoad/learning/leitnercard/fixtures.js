function() {
    var fixtureContainer = karate.read('classpath:com/mnemotoad/learning/common/fixture-container.js');

    return fixtureContainer({
        create: function(overrides) {
            return karate.call('classpath:com/mnemotoad/learning/leitnercard/create-leitnercard.feature', overrides || {});
        },
        remove: function(leitnerCardId) {
            karate.call('classpath:com/mnemotoad/learning/leitnercard/delete-leitnercard.feature', { leitnerCardId: leitnerCardId });
        }
    });
}
