Feature: Station order summary

Background:
    Given station:
    | RegionId | LocationId | Name |
    | 10000002 | 60003760   | Jita |
    And item type '1230' - 'Veldspar'
    
Scenario: Do not refresh if current summary is still ok to use
    Given order summary:
    | Item         | Price | VolumeRemaining | ShouldBeUsedForBuybackCalculations | ExpirationDateTime   |
    | Veldspar     | 0.10  | 1000000         | true                               | 2022-04-18T00:00:00  |
    When refreshing order summary for item 'Veldspar' and a volume of '10' at '2022-04-17T00:00:00'
    Then refresh aborted because summary is still valid

Scenario: Do not refresh if item is invalid
    Given order summary:
    | Item         | Price | VolumeRemaining | ShouldBeUsedForBuybackCalculations | ExpirationDateTime   |
    | Veldspar     | 0.10  | 1000000         | true                               | 2022-04-18T00:00:00  |
    When refreshing order summary for item 'unkown' and a volume of '10' at '2022-04-17T00:00:00'
    Then refresh aborted because item is not valid

Scenario: Do not refresh if current summary is still ok to use even if it should not be used for buyback calculations
    Given order summary:
    | Item         | Price | VolumeRemaining | ShouldBeUsedForBuybackCalculations | ExpirationDateTime   |
    | Veldspar     | 0.10  | 1000000         | false                              | 2022-04-18T00:00:00  |
    When refreshing order summary for item 'Veldspar' and a volume of '10' at '2022-04-17T00:00:00'
    Then refresh aborted because summary is still valid

Scenario: Refresh if no current summary exists                         | 2022-04-18T00:00:00  |
    When refreshing order summary for item 'Veldspar' and a volume of '10' at '2022-04-18T00:00:01'
    Then refresh marked current summary version as invalid

Scenario: Refresh if current summary is expired
    Given order summary:
    | Item         | Price | VolumeRemaining | ShouldBeUsedForBuybackCalculations | ExpirationDateTime   |
    | Veldspar     | 0.10  | 1000000         | true                               | 2022-04-18T00:00:00  |
    When refreshing order summary for item 'Veldspar' and a volume of '10' at '2022-04-18T00:00:01'
    Then refresh marked current summary version as invalid

Scenario: Refresh if there is not enough remaining volume
    Given order summary:
    | Item         | Price | VolumeRemaining | ShouldBeUsedForBuybackCalculations | ExpirationDateTime   |
    | Veldspar     | 0.10  | 1               | true                               | 2022-04-18T00:00:00  |
    When refreshing order summary for item 'Veldspar' and a volume of '10' at '2022-04-17T00:00:00'
    Then refresh marked current summary version as invalid