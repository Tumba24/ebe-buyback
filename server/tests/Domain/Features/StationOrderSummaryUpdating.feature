Feature: Station order summary updating

Background:
    Given station:
    | RegionId | LocationId | Name |
    | 10000002 | 60003760   | Jita |
    And item type '1230' - 'Veldspar'

Scenario: Can create default missing order summary
    When updating order summary for item 'Veldspar' and a volume of '10' at '2022-04-18T00:00:00'
    Then updated order summary is:
    | Item         | Price | VolumeRemaining | ShouldBeUsedForBuybackCalculations | ExpirationDateTime   |
    | Veldspar     | 0.00  | 0               | false                              | 2022-04-18T00:00:02  |

Scenario: Can create missing order summary usinig order informaton
    Given order:
    | Item     | IsBuyOrder | Price | DurationInDays | IssuedOnDateTime    | MinVolume | VolumeRemaining | ExpiresOnDateTime   |
    | Veldspar | true       | 10.00 | 3              | 2022-04-18T00:00:00 | 1         | 100             | 2022-04-18T00:00:01 |
    When updating order summary for item 'Veldspar' and a volume of '10' at '2022-04-18T00:00:00'
    Then updated order summary is:
    | Item         | Price  | VolumeRemaining | ShouldBeUsedForBuybackCalculations | ExpirationDateTime   |
    | Veldspar     | 10.00  | 100             | true                               | 2022-04-18T00:00:01  |
