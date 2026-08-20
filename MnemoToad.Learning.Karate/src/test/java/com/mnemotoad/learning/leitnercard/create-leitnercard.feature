@ignore
Feature: Create a LeitnerCard (reusable setup helper)

  Background:
    * url baseUrl

  Scenario:
    * def uniqueName = read('classpath:com/mnemotoad/learning/common/util.js')
    * def deckId = karate.get('deckId')
    * assert deckId != null
    * def nodeId = karate.get('nodeId')
    * def properties = karate.get('properties') ? karate.get('properties') : { _canonicalName: uniqueName('LeitnerCard') }
    Given path 'leitner/cards'
    And request { deckId: '#(deckId)', cards: [{ nodeId: '#(nodeId)', properties: '#(properties)' }] }
    When method post
    Then status 201
    * def response = response.cards[0]
