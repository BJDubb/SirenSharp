# 🧪 Test in-game

The #1 support question is "it plays in OpenIV but not in-game". Most of the time
that's LVC / VCF config, not the SirenSharp pack. The in-game tester lets you
confirm the pack itself works **with no LVC setup at all**, so you know where to
look next.

## Generate the tester

On the **Generate Resource** dialog, leave **Generate in-game tester** ticked.
Alongside your pack, SirenSharp writes a `sirensharp-audio-test` resource with its
`config.lua` already filled in from your soundset and siren names - nothing to
hand-edit.

## Run it

1. Drop both folders into your server's resources:
   - your generated pack
   - `sirensharp-audio-test`
2. `ensure` your pack **first**, then `ensure sirensharp-audio-test`.
3. In game, run `/sirentest` to open the menu.
4. Pick a soundset, then a siren - it plays from your vehicle (or your ped).
   **Stop** / **Stop all** halt it. **2D (frontend) mode** plays it non-positionally
   if you want to confirm the bank loaded.

Each play also prints the exact native names and the LVC `Bank` path to the F8
console, so you can line them up against your VCF later:

* `String` / `Ref` = the siren name
* `Bank` = `dlc_<dlcname>/<soundset>`

## What the result tells you

| Plays in CodeWalker/OpenIV? | Plays via the tester? | What it means |
| --- | --- | --- |
| No | - | Bad source WAVs. Fix to mono 16-bit PCM (Fix Audio) and regenerate. |
| Yes | No | FiveM streaming or the resource isn't started. Check `ensure` order and run `/refresh`. |
| Yes | Yes | The pack is good. Any remaining problem is LVC / VCF config - see [Troubleshooting](../info/troubleshooting.md). |

{% hint style="info" %}
The tester bundles a pinned copy of [RageUI](https://github.com/ImBaphomettt/RageUI)
so it runs on its own with no extra dependencies.
{% endhint %}
