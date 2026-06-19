# Troubleshooting

## Sirens work in OpenIV but not in-game (LVC Fleet)

This is the **#1 support issue** and is usually **not** a SirenSharp bug.

1. Set `Local_Override Enabled="false"` in every relevant LVC VCF XML.
2. Also disable Local Override in the LVC in-game O-menu.
3. Verify VCF `String`, `Ref`, and `Bank` match your SirenSharp resource paths.
4. Run `/refresh` and ensure the resource is started.

See [LVC Fleet docs](https://docs.luxartengineering.com/fleet/resource-installation/configure-base-settings).

## Silent or ~1 KB AWC files

**Cause:** Incompatible source WAV (stereo, wrong bit depth, junk RIFF headers).

**Fix in v0.4:** Use **Fix Audio** on the siren, or regenerate - SirenSharp auto-converts on export. You can also re-export from Audacity as *WAV (Microsoft) signed 16-bit PCM* mono.

## AWC shows hex names in OpenIV

**Expected behaviour.** Hex names are part of AWC encoding. Use your **string siren names** in LVC VCF - not the hex values shown in OpenIV.

## Downloaded source code instead of the app

If `SirenSharp.exe` is missing or generation fails silently, you likely downloaded **Source code** from GitHub instead of the release ZIP. Get [SirenSharp-v0.x.zip](https://github.com/BJDubb/SirenSharp/releases/latest).

## Testing without LVC

Tick **Generate in-game tester** on the export dialog and SirenSharp writes a `sirensharp-audio-test` resource next to your pack. Run `/sirentest` in-game to play your soundsets through a menu with no LVC configuration. See [Test in-game](../getting-started/test-in-game.md).

## Game build / fx_version

Custom AWC/dat54 audio resources are generally **game-build agnostic**. `sv_enforceGameBuild` is a server setting, not something in the resource manifest. Use the default `cerulean` fx_version unless your server docs say otherwise.
