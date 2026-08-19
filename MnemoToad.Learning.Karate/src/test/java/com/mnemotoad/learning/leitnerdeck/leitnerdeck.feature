@Regression @LeitnerDeck
Feature: LeitnerDeck API

  Background:
    * url baseUrl
    * def uniqueName = read('classpath:com/mnemotoad/learning/common/util.js')
    * def leitnerDeckFixtures = call read('fixtures.js')
    * def createLeitnerDeck = leitnerDeckFixtures.create
    * configure afterScenario = leitnerDeckFixtures.cleanup

  Scenario: Create a deck successfully
    * def name = uniqueName('LeitnerDeck')
    Given path 'leitner/decks'
    And request { name: '#(name)', description: 'Created by Karate test' }
    When method post
    Then status 201
    And match response.name == name
    And match response.description == 'Created by Karate test'
    And match response.id == '#uuid'
    * eval leitnerDeckFixtures.stageForCleanup(response.id)

  Scenario: Create multiple decks
    * def name1 = uniqueName('LeitnerDeck')
    * def name2 = uniqueName('LeitnerDeck')
    * def name3 = uniqueName('LeitnerDeck')

    Given path 'leitner/decks'
    And request { name: '#(name1)' }
    When method post
    Then status 201
    * eval leitnerDeckFixtures.stageForCleanup(response.id)
    * def id1 = response.id

    Given path 'leitner/decks'
    And request { name: '#(name2)' }
    When method post
    Then status 201
    * eval leitnerDeckFixtures.stageForCleanup(response.id)
    * def id2 = response.id

    Given path 'leitner/decks'
    And request { name: '#(name3)' }
    When method post
    Then status 201
    * eval leitnerDeckFixtures.stageForCleanup(response.id)
    * def id3 = response.id

    * match id1 != id2
    * match id2 != id3
    * match id1 != id3

    Given path 'leitner/decks'
    When method get
    Then status 200
    * def foundNames = karate.map(karate.filter(response, function(x){ return x.name == name1 || x.name == name2 || x.name == name3 }), function(x){ return x.name })
    * match foundNames contains name1
    * match foundNames contains name2
    * match foundNames contains name3

  Scenario: Reject creation with missing name
    Given path 'leitner/decks'
    And request { name: '', description: 'should fail' }
    When method post
    Then status 400

  Scenario: Create a deck with a name that already exists
    * def name = uniqueName('LeitnerDeck')
    * def created = createLeitnerDeck({ name: name })

    Given path 'leitner/decks'
    And request { name: '#(name)' }
    When method post
    Then status 201
    And match response.id != created.response.id
    * eval leitnerDeckFixtures.stageForCleanup(response.id)

  Scenario: Get a deck by id
    * def created = createLeitnerDeck()

    Given path 'leitner/decks', created.response.id
    When method get
    Then status 200
    And match response.name == created.response.name

  Scenario: List decks includes the newly created one
    * def created = createLeitnerDeck()

    Given path 'leitner/decks'
    When method get
    Then status 200
    * def found = karate.filter(response, function(x){ return x.name == created.response.name })
    And match found[0].name == created.response.name

  Scenario: Update a deck
    * def created = createLeitnerDeck()

    * def updatedName = created.response.name + '_Updated'
    Given path 'leitner/decks', created.response.id
    And request { name: '#(updatedName)', description: 'Updated by test' }
    When method put
    Then status 200
    And match response.name == updatedName
    And match response.description == 'Updated by test'

  Scenario: Delete a deck
    * def created = createLeitnerDeck()

    Given path 'leitner/decks', created.response.id
    When method delete
    Then status 204

    Given path 'leitner/decks', created.response.id
    When method get
    Then status 404
