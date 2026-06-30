# ⚙️ Installing SirenSharp

SirenSharp is **Windows only**.

## Step 1 - Download

Go to [GitHub Releases](https://github.com/BJDubb/SirenSharp/releases/latest) and pick one:

* **Recommended - installer:** download the **Setup** `.exe`. It installs SirenSharp and then **updates itself automatically** in the background, so you don't have to re-download for every new version.
* **Portable:** download the **portable** `.zip` if you'd rather not install. No auto-update - you re-download to upgrade.

{% hint style="danger" %}
Do **not** download "Source code" - that is for developers. Use the Setup `.exe` or the portable `.zip`.
{% endhint %}

## Step 2 - Run

* **Installer:** run the Setup `.exe`. SirenSharp installs and launches itself; a desktop/start-menu shortcut is created. It's **self-contained** - you don't need to install .NET or anything else.
* **Portable:** extract the `.zip` and run `SirenSharp.exe`.

## Step 3 - Optional tools for verification

* **CodeWalker** or **OpenIV** - inspect generated AWC files (SirenSharp can launch CodeWalker after generation if installed)
* **Audacity** - only if you want to edit source audio by hand; SirenSharp already auto-converts WAVs on export
