# sirensharp-audio-test

A standalone FiveM resource for confirming a SirenSharp pack plays **in-game**,
with no LVC / VCF configuration. It opens an in-game [RageUI](https://github.com/ImBaphomettt/RageUI)
menu where you browse the pack's soundsets, pick a siren, and play/stop it.

Use it to isolate the question *"is the SirenSharp output actually good?"* from
server-side audio config (LVC Fleet, VCF assignments, etc.).

## Install

1. Drop both resources into your server:
   - your generated SirenSharp pack (the one with `fxmanifest.lua` + `data/` + `dlc_*/`)
   - this `sirensharp-audio-test` resource
2. `ensure` your pack **before** the tester, then `ensure sirensharp-audio-test`.
3. Edit `config.lua` so `Config.Soundsets` matches your pack's DLC / soundset /
   siren names. SirenSharp can write this for you: **Generate Resource > "Generate
   in-game tester"** emits a copy of this resource with `config.lua` pre-filled.

## Use

- In game, run `/sirentest` (or set `Config.Keybind`) to open the menu.
- Top level lists your soundsets; select one to see its sirens.
- Selecting a siren plays it from your vehicle (or ped); **Stop** / **Stop all** halt it.
- **2D (frontend) mode** plays through `PlaySoundFrontend` - handy to confirm the
  bank loaded even if positional audio seems silent.
- Every play echoes the exact native names + LVC `Bank` path to the F8 console so
  you can line them up against your VCF (`String`/`Ref` = siren name, `Bank` = the dlc path).

## Diagnosing

| Plays in CodeWalker/OpenIV? | Plays via this tester? | Conclusion |
| --- | --- | --- |
| No | - | Bad WAVs - fix to mono 16-bit PCM and regenerate. |
| Yes | No | FiveM streaming / resource not started / needs `/refresh`. |
| Yes | Yes | The pack is good - any remaining issue is LVC/VCF config. |

## RageUI

RageUI is vendored (pinned, unmodified) under `RageUI/` because it is archived
upstream. See `RageUI/VENDORED.md` for the source and commit.
