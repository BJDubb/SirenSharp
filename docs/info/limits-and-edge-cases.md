# ⚠️ Limits & Edge Cases

Most "it builds fine but won't play" problems come down to a handful of causes. Each one below is a **symptom → why → what to do**.

## A siren is silent in-game

**Why:** the game finds sirens by a **hash of the name**, not the text. If the name in your LVC/VCF config doesn't match the name in the pack exactly (lowercased), the game looks up nothing and you get silence. A typo, a stray space, or different casing is enough.

**Fix:** make the name in your config match the siren name in SirenSharp exactly. See [Naming Requirements](naming-requirements.md).

## Two AWCs clash and one goes silent

**Why:** the game only looks at the **first 8 characters** of an AWC (soundset) name. Two AWCs that start with the same 8 characters (e.g. `policecar1` and `policecar2` -> both `policeca`) are treated as the **same** pack: one loads, the other plays silent, even though the names look different.

**Fix:** keep the first 8 characters of each AWC name unique. Short names are simplest. SirenSharp warns you about this in preflight.

## A single pack is too big to load

**Why:** each audio pack loads into a fixed-size memory **slot**. If a pack is bigger than its slot, it doesn't fit and plays silent. The limit is **bytes, not seconds** and sirens become mono 16-bit PCM, where `bytes ≈ sample_rate × 2 × seconds`. So a **lower sample rate fits more audio**. (Note: the widely-repeated "~7 seconds" figure isn't a real engine value — there's no single fixed per-siren limit. Siren packs are small and rarely hit this.)

**Fix:** **trim** the audio or **lower the sample rate** of the source.

## The server runs out of audio memory (lots of custom audio)

**Why:** all loaded custom audio shares one pool of memory called the **AudioHeap** (default ~195 MB). It's not a fixed *number* of packs — it's memory. When it fills, new packs go silent and the game can crash with:

> `AudioHeap Pool Full ... raise AudioHeap PoolSize in common/data/gameconfig.xml`

This is why people see a wall around **~175–190 custom packs** — that's just the default heap filling up.

## Good to know

* **Audio always ends up mono 16-bit PCM.** SirenSharp auto-converts on export (stereo is downmixed). Stereo separation and high bit depths are lost by design; the source sample rate is kept.
* **Resources are build-agnostic.** Custom AWC/dat54 audio doesn't depend on a specific GTA build. Use the default `cerulean` fx_version unless your server docs say otherwise.
