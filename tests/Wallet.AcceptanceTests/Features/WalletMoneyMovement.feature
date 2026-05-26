Feature: Wallet money movement
  Wallet users must be able to top up, pay, reserve, release, refund, and use promo credit safely.

  Scenario: Top-up makes real balance available
    Given a wallet user exists
    When the user tops up 1000 with idempotency key "topup-real-money"
    Then the wallet available balance should be 1000
    And the wallet reserved balance should be 0
    And General transactions should include TopUp

  Scenario: Fast Pay spends available real balance immediately
    Given a wallet user has topped up 1000
    When the user pays 350 for Food
    Then the wallet available balance should be 650
    And the wallet reserved balance should be 0
    And Food transactions should include Payment

  Scenario: Fast Pay is rejected when available balance is insufficient
    Given a wallet user has topped up 100
    When the user tries to pay 150 for Shop
    Then the operation should be rejected
    And the wallet available balance should be 100
    And the wallet reserved balance should be 0

  Scenario: Reserved money can be confirmed
    Given a wallet user has topped up 1000
    When the user reserves 300 for Travel
    And the user confirms the reservation
    Then the wallet available balance should be 700
    And the wallet reserved balance should be 0
    And Travel transactions should include Hold and Capture

  Scenario: Reserved money can be cancelled and released
    Given a wallet user has topped up 1000
    When the user reserves 300 for Travel
    And the user cancels the reservation
    Then the wallet available balance should be 1000
    And the wallet reserved balance should be 0
    And Travel transactions should include Hold and Release

  Scenario: Expired reservations are released by the worker
    Given a wallet user has topped up 1000
    And the user has an expired Travel reservation of 300
    When the reservation expiry worker runs
    Then the wallet available balance should be 1000
    And the wallet reserved balance should be 0
    And Travel transactions should include Hold and Release

  Scenario: Refund restores real balance
    Given a wallet user has topped up 1000
    And the user has paid 400 for Food
    When the user receives a refund of 150 for Food
    Then the wallet available balance should be 750
    And Food transactions should include Payment and Refund

  Scenario: Promo credit is service scoped and consumable
    Given a wallet user has topped up 100
    And the user has Food promo credit of 200
    When the user consumes 80 Food promo credit
    Then Food promo remaining balance should be 120
    And Food transactions should include PromoConsume

  Scenario: Promo credit cannot be overspent
    Given a wallet user has topped up 100
    And the user has Food promo credit of 50
    When the user tries to consume 80 Food promo credit
    Then the operation should be rejected
    And Food promo remaining balance should be 50

  Scenario: Retried top-up with the same idempotency key is not duplicated
    Given a wallet user exists
    When the user tops up 250 with idempotency key "same-topup"
    And the user tops up 250 again with idempotency key "same-topup"
    Then the wallet available balance should be 250
    And only 1 ledger transaction should exist

  Scenario: Concurrent payments cannot overspend the wallet
    Given a wallet user has topped up 100
    When 10 concurrent Shop payments of 30 are submitted
    Then the wallet available balance should never be negative
    And at most 3 Shop payments should be successful
