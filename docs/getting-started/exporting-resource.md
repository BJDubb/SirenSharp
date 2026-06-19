# 📂 Exporting Resource

When your project is ready, click **Generate Resource** in the toolbar.

Fill in:

* **Resource Name** - the FiveM resource folder name
* **DLC Name** - without the `dlc_` prefix; must be unique across your SirenSharp projects
* **fx_version** - default `cerulean` (change only if your server requires it)
* **Output location** - where the resource folder is written
* **Generate in-game tester** - also writes a `sirensharp-audio-test` resource (see [Test in-game](test-in-game.md))

SirenSharp generates:

```
your-resource/
  fxmanifest.lua
  SIRENSHARP_NOTES.txt
  data/
    {dlc}.dat54.rel
    {dlc}.dat54.nametable
  dlc_{dlc}/
    {soundset}.awc
```

After generation, use **Open Folder** or **RPF Explorer** (launches CodeWalker) to inspect the output before loading FiveM. To hear the sirens in-game, see [Test in-game](test-in-game.md).

{% hint style="info" %}
Note the DLC name - you need it when configuring LVC Fleet VCF files.
{% endhint %}

{% hint style="warning" %}
FiveM may only support **7 concurrent audio banks**. If you have more than 7 AWC soundsets, SirenSharp will warn you before generating.
{% endhint %}
