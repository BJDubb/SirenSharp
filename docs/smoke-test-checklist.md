# Smoke-test checklist

Manual start-to-finish check to run before tagging a release. Covers the full
flow the automated tests can't drive (file dialogs, drag-and-drop, modal
windows). Screenshot markers (📷) call out the screens worth capturing for the
GitBook docs.

Prep: have two WAVs handy - one clean **mono 16-bit PCM** and one **stereo** (or
24-bit) so the format warning / Fix Audio path can be exercised.

## 1. Launch
- [ ] `SirenSharp.exe` starts; main window title reads `SirenSharp vX.Y`. 📷 (empty main window)
- [ ] Dark theme renders; toolbar buttons that need a project are disabled.

## 2. New project
- [ ] **New** opens the New Project dialog; name + folder validate. 📷
- [ ] Creating writes `<name>.ssproj` to disk; title shows the project name.
- [ ] Re-creating with the same name in the same folder prompts to overwrite.

## 3. Create an AWC (soundset)
- [ ] **New** under the AWC list adds a soundset; selecting it shows its inspector.
- [ ] Renaming to an invalid name (spaces/caps) surfaces a validation error. 📷
- [ ] Two soundsets with the same name flag a project error.

## 4. Add sirens
- [ ] **Import WAV** (toolbar) multi-selects and adds WAVs to the selected soundset. 📷
- [ ] Drag-and-drop `.wav` files onto the siren list also imports them.
- [ ] **New** adds a placeholder siren; **Browse** assigns a WAV to it.
- [ ] Importing the stereo/24-bit WAV shows a format warning badge + status text.

## 5. Fix audio
- [ ] **Fix Audio** on a flagged siren converts it; the warning clears. 📷
- [ ] Converted file lands in the `<ProjectName>_files` folder next to the project.

## 6. Preview
- [ ] **Play WAV** plays the source; **Stop** halts it.
- [ ] Play/Stop also work from the per-row siren buttons.

## 7. Save / dirty tracking
- [ ] Editing marks the title with `*` (unsaved).
- [ ] **Save** clears the `*`; **Save As** writes a copy and re-points the project.

## 8. Generate resource
- [ ] **Generate Resource** opens the export dialog pre-filled (resource name, DLC, fx_version, output). 📷
- [ ] >7 soundsets warns about the 7-bank limit before generating.
- [ ] Existing output folder prompts to replace.
- [ ] Progress window shows status messages, then the Result window appears. 📷
- [ ] On success: AWC verification summaries listed; **Open Folder** and **RPF Explorer** work.
- [ ] On a deliberately broken input: failure result lists actionable errors.

## 9. Output verification
- [ ] Output contains `fxmanifest.lua`, `SIRENSHARP_NOTES.txt`, `data/{dlc}.dat54.rel` + `.nametable`, `dlc_{dlc}/{soundset}.awc`.
- [ ] `raw/` working folder is gone.
- [ ] AWC opens in CodeWalker/OpenIV and plays (not ~1 KB / silent).

## 10. Open / recent / close
- [ ] **Open** lists recent projects; opening one restores soundsets + sirens with format badges.
- [ ] Closing with unsaved changes prompts Save / Don't save / Cancel; Cancel keeps the app open.

## 11. Help / About
- [ ] **Help** opens with WAV requirements + the in-game tester reference. 📷
- [ ] **About** shows version and working links (GitHub, Discord, Docs).
