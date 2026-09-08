# Trader Level Requirements Editor

A very small mod I created to customize trader loyalty level requirements since you're no longer able to play with modified SPT data files

## Features

- Changes the player level required for each trader loyalty level

- Changes the total sales amount required

- Changes the reputation or standing required

- Supports configuring individual traders and loyalty levels
  - you can add custom traders if you'd like, but there's no purpose since you can just modify a custom/modded trader's files directly (this should be obvious but I feel it's worth mentioning regardless)

- Logs missing traders and invalid configuration entries

## Installation

1. Copy the mod folder into:

   `SPT_Runtime/user/mods`

## Configuration

Edit:

`config/traders.json`

Each trader is identified by their trader ID and in-game name. Loyalty levels are represented with numbers `"1"`, `"2"`, `"3"`, and `"4"`.

```json
{
  "traders": {
    "TRADER_ID": {
      "name": "Trader Name",
      "loyaltyLevels": {
        "2": {
          "minLevel": 15,
          "minSalesSum": 2500000,
          "minStanding": 0.15
        }
      }
    }
  }
}