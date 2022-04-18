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

Scenario: Can create missing order summary using order informaton
    Given order:
    | Item     | IsBuyOrder | Price | DurationInDays | IssuedOnDateTime    | MinVolume | VolumeRemaining | ExpiresOnDateTime   |
    | Veldspar | true       | 10.00 | 3              | 2022-04-18T00:00:00 | 1         | 100             | 2022-04-18T00:00:01 |
    When updating order summary for item 'Veldspar' and a volume of '10' at '2022-04-18T00:00:00'
    Then updated order summary is:
    | Item         | Price  | VolumeRemaining | ShouldBeUsedForBuybackCalculations | ExpirationDateTime   |
    | Veldspar     | 10.00  | 100             | true                               | 2022-04-18T00:00:01  |

Scenario: Order summary created using highest price
    Given order:
    | Item     | IsBuyOrder | Price | DurationInDays | IssuedOnDateTime    | MinVolume | VolumeRemaining | ExpiresOnDateTime   |
    | Veldspar | true       | 10.00 | 3              | 2022-04-18T00:00:00 | 1         | 100             | 2022-04-18T00:00:01 |
    And order:
    | Item     | IsBuyOrder | Price | DurationInDays | IssuedOnDateTime    | MinVolume | VolumeRemaining | ExpiresOnDateTime   |
    | Veldspar | true       | 11.00 | 3              | 2022-04-18T00:00:00 | 1         | 100             | 2022-04-18T00:00:01 |
    When updating order summary for item 'Veldspar' and a volume of '10' at '2022-04-18T00:00:00'
    Then updated order summary is:
    | Item         | Price  | VolumeRemaining | ShouldBeUsedForBuybackCalculations | ExpirationDateTime   |
    | Veldspar     | 11.00  | 100             | true                               | 2022-04-18T00:00:01  |

Scenario: Order summary created using highest price for total volume remaining
    Given order:
    | Item     | IsBuyOrder | Price | DurationInDays | IssuedOnDateTime    | MinVolume | VolumeRemaining | ExpiresOnDateTime   |
    | Veldspar | true       | 10.00 | 3              | 2022-04-18T00:00:00 | 1         | 100             | 2022-04-18T00:00:01 |
    And order:
    | Item     | IsBuyOrder | Price | DurationInDays | IssuedOnDateTime    | MinVolume | VolumeRemaining | ExpiresOnDateTime   |
    | Veldspar | true       | 11.00 | 3              | 2022-04-18T00:00:00 | 1         | 100             | 2022-04-18T00:00:01 |
    When updating order summary for item 'Veldspar' and a volume of '101' at '2022-04-18T00:00:00'
    Then updated order summary is:
    | Item         | Price  | VolumeRemaining | ShouldBeUsedForBuybackCalculations | ExpirationDateTime   |
    | Veldspar     | 10.00  | 200             | true                               | 2022-04-18T00:00:01  |

Scenario: Order summary should not be used if there is not enough total volume remaining
    Given order:
    | Item     | IsBuyOrder | Price | DurationInDays | IssuedOnDateTime    | MinVolume | VolumeRemaining | ExpiresOnDateTime   |
    | Veldspar | true       | 10.00 | 3              | 2022-04-18T00:00:00 | 1         | 100             | 2022-04-18T00:00:01 |
    And order:
    | Item     | IsBuyOrder | Price | DurationInDays | IssuedOnDateTime    | MinVolume | VolumeRemaining | ExpiresOnDateTime   |
    | Veldspar | true       | 11.00 | 3              | 2022-04-18T00:00:00 | 1         | 100             | 2022-04-18T00:00:01 |
    When updating order summary for item 'Veldspar' and a volume of '201' at '2022-04-18T00:00:00'
    Then updated order summary is:
    | Item         | Price  | VolumeRemaining | ShouldBeUsedForBuybackCalculations | ExpirationDateTime   |
    | Veldspar     | 10.00  | 200             | false                              | 2022-04-18T00:00:01  |

Scenario: Order summary created using latest expiration date time if price is the same
    Given order:
    | Item     | IsBuyOrder | Price | DurationInDays | IssuedOnDateTime    | MinVolume | VolumeRemaining | ExpiresOnDateTime   |
    | Veldspar | true       | 10.00 | 3              | 2022-04-18T00:00:00 | 1         | 100             | 2022-04-18T00:00:01 |
    And order:
    | Item     | IsBuyOrder | Price | DurationInDays | IssuedOnDateTime    | MinVolume | VolumeRemaining | ExpiresOnDateTime   |
    | Veldspar | true       | 10.00 | 3              | 2022-04-18T00:00:00 | 1         | 100             | 2022-04-18T00:00:02 |
    When updating order summary for item 'Veldspar' and a volume of '10' at '2022-04-18T00:00:00'
    Then updated order summary is:
    | Item         | Price  | VolumeRemaining | ShouldBeUsedForBuybackCalculations | ExpirationDateTime   |
    | Veldspar     | 10.00  | 100             | true                               | 2022-04-18T00:00:02  |

Scenario: Order summary created should group expirations together and use the first to match the specified volume
    Given order:
    | Item     | IsBuyOrder | Price | DurationInDays | IssuedOnDateTime    | MinVolume | VolumeRemaining | ExpiresOnDateTime   |
    | Veldspar | true       | 10.00 | 3              | 2022-04-18T00:00:00 | 1         | 100             | 2022-04-18T00:00:01 |
    And order:
    | Item     | IsBuyOrder | Price | DurationInDays | IssuedOnDateTime    | MinVolume | VolumeRemaining | ExpiresOnDateTime   |
    | Veldspar | true       | 10.00 | 3              | 2022-04-18T00:00:00 | 1         | 100             | 2022-04-18T00:00:02 |
    When updating order summary for item 'Veldspar' and a volume of '101' at '2022-04-18T00:00:00'
    Then updated order summary is:
    | Item         | Price  | VolumeRemaining | ShouldBeUsedForBuybackCalculations | ExpirationDateTime   |
    | Veldspar     | 10.00  | 200             | true                               | 2022-04-18T00:00:01  |

Scenario: Order summary should not consider orders that have already expired
    Given order:
    | Item     | IsBuyOrder | Price | DurationInDays | IssuedOnDateTime    | MinVolume | VolumeRemaining | ExpiresOnDateTime   |
    | Veldspar | true       | 10.00 | 3              | 2022-04-18T00:00:00 | 1         | 100             | 2022-04-18T00:00:01 |
    And order:
    | Item     | IsBuyOrder | Price | DurationInDays | IssuedOnDateTime    | MinVolume | VolumeRemaining | ExpiresOnDateTime   |
    | Veldspar | true       | 11.00 | 3              | 2022-04-18T00:00:00 | 1         | 100             | 2022-04-18T00:00:00 |
    When updating order summary for item 'Veldspar' and a volume of '100' at '2022-04-18T00:00:00'
    Then updated order summary is:
    | Item         | Price  | VolumeRemaining | ShouldBeUsedForBuybackCalculations | ExpirationDateTime   |
    | Veldspar     | 10.00  | 100             | true                               | 2022-04-18T00:00:01  |

Scenario: Order summary should not be used if there is not enough total volume remaining that has not expired
    Given order:
    | Item     | IsBuyOrder | Price | DurationInDays | IssuedOnDateTime    | MinVolume | VolumeRemaining | ExpiresOnDateTime   |
    | Veldspar | true       | 10.00 | 3              | 2022-04-18T00:00:00 | 1         | 100             | 2022-04-18T00:00:01 |
    And order:
    | Item     | IsBuyOrder | Price | DurationInDays | IssuedOnDateTime    | MinVolume | VolumeRemaining | ExpiresOnDateTime   |
    | Veldspar | true       | 11.00 | 3              | 2022-04-18T00:00:00 | 1         | 100             | 2022-04-18T00:00:00 |
    When updating order summary for item 'Veldspar' and a volume of '101' at '2022-04-18T00:00:00'
    Then updated order summary is:
    | Item         | Price  | VolumeRemaining | ShouldBeUsedForBuybackCalculations | ExpirationDateTime   |
    | Veldspar     | 10.00  | 100             | false                              | 2022-04-18T00:00:01  |

Scenario: Order summary excludes orders where where min volume is too high
    Given order:
    | Item     | IsBuyOrder | Price | DurationInDays | IssuedOnDateTime    | MinVolume | VolumeRemaining | ExpiresOnDateTime   |
    | Veldspar | true       | 10.00 | 3              | 2022-04-18T00:00:00 | 1         | 100             | 2022-04-18T00:00:01 |
    And order:
    | Item     | IsBuyOrder | Price | DurationInDays | IssuedOnDateTime    | MinVolume | VolumeRemaining | ExpiresOnDateTime   |
    | Veldspar | true       | 11.00 | 3              | 2022-04-18T00:00:00 | 11        | 100             | 2022-04-18T00:00:01 |
    When updating order summary for item 'Veldspar' and a volume of '10' at '2022-04-18T00:00:00'
    Then updated order summary is:
    | Item         | Price  | VolumeRemaining | ShouldBeUsedForBuybackCalculations | ExpirationDateTime   |
    | Veldspar     | 10.00  | 100             | true                               | 2022-04-18T00:00:01  |

Scenario: Order summary excludes orders where where min volume is too high after taking better prices into account 
    Given order:
    | Item     | IsBuyOrder | Price | DurationInDays | IssuedOnDateTime    | MinVolume | VolumeRemaining | ExpiresOnDateTime   |
    | Veldspar | true       | 10.00 | 3              | 2022-04-18T00:00:00 | 1         | 100             | 2022-04-18T00:00:01 |
    And order:
    | Item     | IsBuyOrder | Price | DurationInDays | IssuedOnDateTime    | MinVolume | VolumeRemaining | ExpiresOnDateTime   |
    | Veldspar | true       | 11.00 | 3              | 2022-04-18T00:00:00 | 11        | 100             | 2022-04-18T00:00:01 |
    And order:
    | Item     | IsBuyOrder | Price | DurationInDays | IssuedOnDateTime    | MinVolume | VolumeRemaining | ExpiresOnDateTime   |
    | Veldspar | true       | 12.00 | 3              | 2022-04-18T00:00:00 | 11        | 13              | 2022-04-18T00:00:01 |
    When updating order summary for item 'Veldspar' and a volume of '14' at '2022-04-18T00:00:00'
    Then updated order summary is:
    | Item         | Price  | VolumeRemaining | ShouldBeUsedForBuybackCalculations | ExpirationDateTime   |
    | Veldspar     | 11.00  | 100             | true                               | 2022-04-18T00:00:01  |