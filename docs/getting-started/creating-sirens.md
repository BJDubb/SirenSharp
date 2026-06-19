# 🚨 Creating Sirens

Once you have [created an AWC](creating-awcs.md) (soundset), add sirens to it.
Select a soundset, then drag and drop `.wav` files anywhere onto the **Sirens**
panel, or click **Import WAV**.

You can also click **Add** to create a placeholder siren, then assign a WAV to it
with **Browse** in the inspector on the right.

{% hint style="info" %}
**WAV requirements:** Mono channel, 16-bit PCM encoding. SirenSharp shows a format
badge on each siren and can **Fix Audio** in one click, or auto-convert during
generation.
{% endhint %}

{% hint style="warning" %}
Stereo, 24-bit, or WAVs with extra junk headers (common from third-party packs)
caused silent AWC output in older versions. SirenSharp now sanitizes audio
automatically before encoding.
{% endhint %}

Select a siren and use **Play** in the inspector's Preview section to hear the
source file before generating.
