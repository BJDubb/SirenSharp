# 📜 Naming Requirements

All names are **lowercased automatically** before the pack is built.

## Why names matter (how the game finds your sirens)

The game does **not** store your siren's text name inside the AWC. It stores a **hash** of the name (a Jenkins one-at-a-time hash of the lowercased string). The dat54 metadata references each wave by hashing the **same** lowercased name. At runtime the game hashes the name from the dat54/your LVC config and looks that hash up in the bank.

This has two consequences:

* **Case doesn't matter, but it must be consistent.** SirenSharp lowercases everything so the AWC hash and the dat54 reference always match. (Older builds didn't, which is why some packs were silent in-game - see [Troubleshooting](troubleshooting.md).)
* **The name you put in LVC must match exactly** (lowercased). A typo = a different hash = the game finds nothing and the siren is silent.

## Allowed characters

The in-app validator only blocks **empty** names and **spaces**, but the safe, supported character set is:

* **Lowercase letters `a-z`, numbers `0-9`, and underscores `_`**
* No spaces, no `dlc_` prefix on the DLC name

Other characters (`-`, `.`, `()`, accents, etc.) may technically hash, but they make LVC/VCF string matching error-prone and some tools assume `[a-z0-9_]`. **Stick to `a-z 0-9 _`.**

### AWC (soundset) names
* Example: `lspd`, `bcso`, `fire_amb`

{% hint style="warning" %}
**The game identifies a wavepack (AWC) by the first 8 characters of its name.** Anything past the 8th character is effectively ignored when the game looks the bank up (this is why FiveM's wavepack handling matches base packs on 8 characters). A longer name still works **as long as its first 8 characters are unique** - but two AWCs that share the same first 8 characters (e.g. `policecar1` and `policecar2`, both `policeca`) resolve to the **same** bank: only one loads and the other plays **silent** in-game, even though the names look different. SirenSharp flags this collision in preflight. Keep the first 8 characters of each AWC name distinct (short names are simplest).
{% endhint %}

### Siren names
* Derived from the source filename on import (spaces become underscores)
* Example: `wail`, `yelp`, `airhorn`

### DLC name (export dialog)
* Do **not** include the `dlc_` prefix - SirenSharp adds it
* Must be unique across your SirenSharp projects
* Example: `policesirens` → output folder `dlc_policesirens/`

## Avoid near-duplicate names

Because waves are stored by a 29-bit slice of the name hash, two **different** names can - very rarely - collide to the same id, and one siren will shadow the other. If a specific siren won't play but its neighbours do, rename it to something distinct.

## FiveM / LVC references

In LVC Fleet VCF, reference:

* **Bank:** `dlc_{dlcname}/{soundset}` (lowercase)
* **String / Ref:** your siren name strings (e.g. `wail`, `yelp`)

AWC hex names in OpenIV are normal - use string names in LVC, not hex.
