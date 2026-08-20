@Regression @LeitnerCard
Feature: LeitnerCard API

  Background:
    * url baseUrl
    * def uniqueName = read('classpath:com/mnemotoad/learning/common/util.js')
    * def leitnerDeckFixtures = call read('classpath:com/mnemotoad/learning/leitnerdeck/fixtures.js')
    * def leitnerCardFixtures = call read('fixtures.js')
    * def createLeitnerDeck = leitnerDeckFixtures.create
    * def createLeitnerCard = leitnerCardFixtures.create
    * configure afterScenario =
      """
      function(){
        // LeitnerCards must be deleted before their referenced LeitnerDecks -- once cards cascade
        // from a deck delete this ordering stops mattering, but it isn't implemented yet.
        leitnerCardFixtures.cleanup();
        leitnerDeckFixtures.cleanup();
      }
      """

  Scenario: Create a card successfully
    * def deck = createLeitnerDeck()
    * def properties = { _canonicalName: 'France', '.population': 68000000 }
    Given path 'leitner/cards'
    And request { deckId: '#(deck.response.id)', cards: [{ properties: '#(properties)' }] }
    When method post
    Then status 201
    And match response.cards[0].deckId == deck.response.id
    And match response.cards[0].id == '#uuid'
    And match response.cards[0].nodeId == '#null'
    And match response.cards[0].boxNumber == 0
    And match response.cards[0].dueUtc == '#present'
    And match response.cards[0].lastReviewedUtc == '#null'
    And match response.cards[0].properties == properties
    * eval leitnerCardFixtures.stageForCleanup(response.cards[0].id)

  Scenario: Create a card with a Knowledge node id
    * def deck = createLeitnerDeck()
    * def nodeId = java.util.UUID.randomUUID() + ''
    Given path 'leitner/cards'
    And request { deckId: '#(deck.response.id)', cards: [{ nodeId: '#(nodeId)', properties: { _canonicalName: 'France' } }] }
    When method post
    Then status 201
    And match response.cards[0].nodeId == nodeId
    * eval leitnerCardFixtures.stageForCleanup(response.cards[0].id)

  Scenario: Create multiple cards into the same deck in one request
    * def deck = createLeitnerDeck()
    Given path 'leitner/cards'
    And request { deckId: '#(deck.response.id)', cards: [{ properties: { _canonicalName: 'France' } }, { properties: { _canonicalName: 'Japan' } }] }
    When method post
    Then status 201
    And match response.cards == '#[2]'
    And match response.cards[0].id != response.cards[1].id
    And match response.cards[0].deckId == deck.response.id
    And match response.cards[1].deckId == deck.response.id
    * eval leitnerCardFixtures.stageForCleanup(response.cards[0].id)
    * eval leitnerCardFixtures.stageForCleanup(response.cards[1].id)

  Scenario: Reject creation with a missing deck id
    Given path 'leitner/cards'
    And request { cards: [{ properties: { _canonicalName: 'France' } }] }
    When method post
    Then status 400

  Scenario: Reject creation with an invalid deck id
    Given path 'leitner/cards'
    And request { deckId: 'not-a-guid', cards: [{ properties: { _canonicalName: 'France' } }] }
    When method post
    Then status 400

  Scenario: Reject creation with an empty cards array
    * def deck = createLeitnerDeck()
    Given path 'leitner/cards'
    And request { deckId: '#(deck.response.id)', cards: [] }
    When method post
    Then status 400

  Scenario: Reject creation with a card that has no properties
    * def deck = createLeitnerDeck()
    Given path 'leitner/cards'
    And request { deckId: '#(deck.response.id)', cards: [{ properties: {} }] }
    When method post
    Then status 400

  Scenario: Create cards for a deck that does not exist
    * def nonExistentDeckId = java.util.UUID.randomUUID() + ''
    Given path 'leitner/cards'
    And request { deckId: '#(nonExistentDeckId)', cards: [{ properties: { _canonicalName: 'France' } }] }
    When method post
    Then status 404

  Scenario: Get a card by id
    * def deck = createLeitnerDeck()
    * def created = createLeitnerCard({ deckId: deck.response.id })

    Given path 'leitner/cards', created.response.id
    When method get
    Then status 200
    And match response.id == created.response.id
    And match response.properties == created.response.properties

  Scenario: Get a card by id that does not exist
    Given path 'leitner/cards', java.util.UUID.randomUUID() + ''
    When method get
    Then status 404

  Scenario: List cards in a deck returns only that deck's cards
    * def deck1 = createLeitnerDeck()
    * def deck2 = createLeitnerDeck()
    * def created1 = createLeitnerCard({ deckId: deck1.response.id })
    * def created2 = createLeitnerCard({ deckId: deck2.response.id })

    Given path 'leitner/cards'
    And param deckId = deck1.response.id
    When method get
    Then status 200
    * def foundIds = karate.map(response, function(x){ return x.id })
    And match foundIds contains created1.response.id
    And match foundIds !contains created2.response.id

  Scenario: List cards in a deck with no cards returns an empty list
    * def deck = createLeitnerDeck()

    Given path 'leitner/cards'
    And param deckId = deck.response.id
    When method get
    Then status 200
    And match response == '#[0]'

  Scenario: List cards for a deck that does not exist
    Given path 'leitner/cards'
    And param deckId = java.util.UUID.randomUUID() + ''
    When method get
    Then status 404

  Scenario: List cards with a missing deck id
    Given path 'leitner/cards'
    When method get
    Then status 400

  Scenario: Delete a card that has properties
    * def deck = createLeitnerDeck()
    * def created = createLeitnerCard({ deckId: deck.response.id, properties: { _canonicalName: 'France', '.population': 68000000 } })

    Given path 'leitner/cards', created.response.id
    When method delete
    Then status 204

    Given path 'leitner/cards', created.response.id
    When method get
    Then status 404

  Scenario: Delete a card that does not exist
    Given path 'leitner/cards', java.util.UUID.randomUUID() + ''
    When method delete
    Then status 404
