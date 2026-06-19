# 📜 Naming Requirements

All names are lowercased automatically.

## AWC (soundset) names

* No spaces
* Lowercase letters, numbers, underscores
* Example: `bcso_sirens`, `lspd_pack`

## Siren names

* No spaces
* Lowercase letters, numbers, underscores
* Derived from WAV filename on import (spaces become underscores)
* Example: `wail`, `yelp`, `airhorn`

## DLC name (export dialog)

* Do **not** include the `dlc_` prefix - SirenSharp adds it
* Must be unique across your SirenSharp projects
* Example: `policesirens` → output folder `dlc_policesirens/`

## FiveM / LVC references

In LVC Fleet VCF, reference:

* **Bank:** `dlc_{dlcname}/{soundset}` (lowercase)
* **String / Ref:** your siren name strings (e.g. `wail`, `yelp`)

AWC hex names in OpenIV are normal - use string names in LVC, not hex.
