 @Smoke @Health
  Feature: Health Check

    Scenario: API is up and healthy
      Given url baseUrl
      And path 'health'
      When method get
      Then status 200
      And match response == {status: "pass"}
