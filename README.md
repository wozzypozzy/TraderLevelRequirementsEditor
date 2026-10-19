# Trader Level Requirements Editor

A very small mod I created to customize trader loyalty level requirements since you're no longer able to play with modified SPT data files

## Features

- Changes the player level required for each trader loyalty level

- Changes the total sales amount required

- Changes the reputation or standing required

- Supports configuring individual traders, loyalty levels, and stock refresh intervals

- Logs missing traders and invalid configuration entries

## Installation

1. Copy the mod folder into:

   `SPT_Runtime/user/mods`

## Configuration

Edit:

`config/traders.jsonc`

Each trader is identified by their trader ID and in-game name. Loyalty levels are represented with numbers `"1"`, `"2"`, `"3"`, and `"4"`. Refresh timers are configured in seconds via `min` and `max` values; omitted values keep the server's existing setting.

Refresh timer values must not be negative, and `min` must not exceed `max`. Use positive values, as zero can cause a trader to refresh immediately (unless that's what you want). Avoid extremely long intervals, as excessively large timestamps may overflow and cause the timer to behave incorrectly.

Avoid adding custom traders to the config. A custom trader's properties can be modified directly so do so there and not here; I haven't tried it myself but I imagine something will break.

```json
{
  "traders": {
    "TRADER_ID": {
      "name": "Trader Name",
      "refreshSeconds": {
        "min": 3600,
        "max": 7200
      },
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