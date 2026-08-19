@ignore
Feature: Create a LeitnerDeck (reusable setup helper)

  Background:
    * url baseUrl

  Scenario:
    * def uniqueName = read('classpath:com/mnemotoad/learning/common/util.js')
    * def name = karate.get('name') ? karate.get('name') : uniqueName('LeitnerDeck')
    * def description = karate.get('description')
    Given path 'leitner/decks'
    And request { name: '#(name)', description: '#(description)' }
    When method post
    Then status 201
