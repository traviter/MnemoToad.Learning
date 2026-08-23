@Regression @LeitnerQuiz
Feature: LeitnerQuiz API

  # Karate has no way to force a card's DueUtc into the past or future -- creation always sets it
  # to "now" (see leitnercard/create-leitnercard.feature) -- so these scenarios can only exercise
  # "immediately due" cards. Not-yet-due filtering and box-number-driven scheduling are covered by
  # the C# system tests instead, which seed LeitnerCards directly into the DB with arbitrary
  # DueUtc/BoxNumber values.

  Background:
    * url baseUrl
    * def leitnerDeckFixtures = call read('classpath:com/mnemotoad/learning/leitnerdeck/fixtures.js')
    * def leitnerCardFixtures = call read('classpath:com/mnemotoad/learning/leitnercard/fixtures.js')
    * def createLeitnerDeck = leitnerDeckFixtures.create
    * def createLeitnerCard = leitnerCardFixtures.create
    * configure afterScenario =
      """
      function(){
        leitnerCardFixtures.cleanup();
        leitnerDeckFixtures.cleanup();
      }
      """

  Scenario: A newly created card is immediately due
    * def deck = createLeitnerDeck()
    * def properties = { _canonicalName: 'France', '.population': 68000000 }
    * def card = createLeitnerCard({ deckId: deck.response.id, properties: properties })

    Given path 'leitner/quiz/due-cards'
    And param deckId = deck.response.id
    When method get
    Then status 200
    And match response == '#[1]'
    And match response[0].id == card.response.id
    And match response[0].boxNumber == 0
    And match response[0].properties == properties
    * def keys = Object.keys(response[0])
    And match keys contains 'id'
    And match keys contains 'boxNumber'
    And match keys contains 'properties'
    And match keys !contains 'deckId'
    And match keys !contains 'nodeId'
    And match keys !contains 'dueUtc'
    And match keys !contains 'lastReviewedUtc'

  Scenario: A deck with no cards returns an empty list
    * def deck = createLeitnerDeck()

    Given path 'leitner/quiz/due-cards'
    And param deckId = deck.response.id
    When method get
    Then status 200
    And match response == '#[0]'

  Scenario: Due cards only include cards from the requested deck
    * def deck1 = createLeitnerDeck()
    * def deck2 = createLeitnerDeck()
    * def card1 = createLeitnerCard({ deckId: deck1.response.id })
    * def card2 = createLeitnerCard({ deckId: deck2.response.id })

    Given path 'leitner/quiz/due-cards'
    And param deckId = deck1.response.id
    When method get
    Then status 200
    * def foundIds = karate.map(response, function(x){ return x.id })
    And match foundIds contains card1.response.id
    And match foundIds !contains card2.response.id

  Scenario: Due cards are ordered earliest-due first
    * def deck = createLeitnerDeck()
    * def card1 = createLeitnerCard({ deckId: deck.response.id })
    * def card2 = createLeitnerCard({ deckId: deck.response.id })

    Given path 'leitner/quiz/due-cards'
    And param deckId = deck.response.id
    When method get
    Then status 200
    * def foundIds = karate.map(response, function(x){ return x.id })
    And assert foundIds.indexOf(card1.response.id) < foundIds.indexOf(card2.response.id)

  Scenario: Properties come back in the order they were submitted
    * def deck = createLeitnerDeck()
    * def properties = { '#flag': { id: java.util.UUID.randomUUID() + '', alt_text: 'The flag of France' }, _canonicalName: 'France', '.population': 68000000 }
    * def card = createLeitnerCard({ deckId: deck.response.id, properties: properties })

    Given path 'leitner/quiz/due-cards'
    And param deckId = deck.response.id
    When method get
    Then status 200
    * def found = karate.filter(response, function(x){ return x.id == card.response.id })[0]
    * def keys = Object.keys(found.properties)
    And match keys == ['#flag', '_canonicalName', '.population']

  Scenario: Get due cards for a deck that does not exist
    Given path 'leitner/quiz/due-cards'
    And param deckId = java.util.UUID.randomUUID() + ''
    When method get
    Then status 404

  Scenario: Get due cards with a missing deck id
    Given path 'leitner/quiz/due-cards'
    When method get
    Then status 400

  Scenario: Get due cards with an invalid deck id
    Given path 'leitner/quiz/due-cards'
    And param deckId = 'not-a-guid'
    When method get
    Then status 400

  # Same DueUtc/BoxNumber limitation as above applies to answers -- box-transition/reschedule
  # detail (jitter windows, box-vs-box-0 resets, explicit overrides) is covered by the C# system
  # tests. This box's happy path relies on the real seeded schedule (box 1 = 24h interval), since
  # Karate runs against a live environment where the DbUp seed script has actually executed.

  Scenario: Reject an empty array of answers
    Given path 'leitner/quiz/cards/answers'
    And request []
    When method post
    Then status 400

  Scenario: Reject an answer missing a card id
    Given path 'leitner/quiz/cards/answers'
    And request [{ correct: true }]
    When method post
    Then status 400

  Scenario: Reject an answer missing correct
    Given path 'leitner/quiz/cards/answers'
    And request [{ cardId: '#(java.util.UUID.randomUUID() + "")' }]
    When method post
    Then status 400

  Scenario: Reject a negative box number
    Given path 'leitner/quiz/cards/answers'
    And request [{ cardId: '#(java.util.UUID.randomUUID() + "")', correct: true, boxNumber: -1 }]
    When method post
    Then status 400

  Scenario: Submit an answer for a card that does not exist
    Given path 'leitner/quiz/cards/answers'
    And request [{ cardId: '#(java.util.UUID.randomUUID() + "")', correct: true }]
    When method post
    Then status 404

  Scenario: A correct answer moves a card out of box 0 and off the due list
    * def deck = createLeitnerDeck()
    * def card = createLeitnerCard({ deckId: deck.response.id })

    Given path 'leitner/quiz/cards/answers'
    And request [{ cardId: '#(card.response.id)', correct: true }]
    When method post
    Then status 204

    Given path 'leitner/cards', card.response.id
    When method get
    Then status 200
    And match response.boxNumber == 1

    Given path 'leitner/quiz/due-cards'
    And param deckId = deck.response.id
    When method get
    Then status 200
    * def foundIds = karate.map(response, function(x){ return x.id })
    And match foundIds !contains card.response.id

  Scenario: An explicit box number above the highest configured box is clamped to it
    * def deck = createLeitnerDeck()
    * def card = createLeitnerCard({ deckId: deck.response.id })

    Given path 'leitner/quiz/cards/answers'
    And request [{ cardId: '#(card.response.id)', correct: true, boxNumber: 999 }]
    When method post
    Then status 204

    Given path 'leitner/cards', card.response.id
    When method get
    Then status 200
    And match response.boxNumber == 8
