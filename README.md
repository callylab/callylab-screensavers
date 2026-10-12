# CallyLab screensavers

Free Windows screensavers from [Cally's Lab](https://callylab.xyz).

| Screensaver | Scene | Audio |
| --- | --- | --- |
| ROME 1.1.0 | Roman forum, moving skies, flames, smoke, fountain spray and rain reflections | Silent |
| EDO 1.1.0 | Tokugawa canal town, rippling water, clouds, smoke and windblown petals | Silent |
| HEARTH 1.1.0 | Medieval English village, clouds, rolling chimney smoke, fire and rain | Silent |
| NIGHTSHIFT 1.0.1 | Original retro city scene and original FM-synth soundtrack | Original music with mute/settings |

## Downloads

Get the ZIPs from [the October 2026 release](https://github.com/callylab/callylab-screensavers/releases/tag/screensavers-2026-10-motion).
Every ZIP includes a per-user `NAME-Setup.exe`, a standalone `NAME.scr`, installation/uninstallation instructions, and SHA-256 checksums. Extract before running setup. Unsigned independent Windows software; Windows may show an unknown-publisher warning.

Install through setup or use the raw `.scr` from a permanent folder. Uninstall in Windows Settings > Apps. Setup preserves sign-in-on-resume and restores the previous screensaver selection when uninstalling the currently selected saver. See [INSTALL.txt](INSTALL.txt).

## Build from source

Windows 10/11 with .NET Framework 4.x. No SDK, npm dependencies, network calls or additional libraries are required. In PowerShell:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
```

Outputs are placed in `release/`. `HistoricalSaver.cs` supplies Windows screensaver modes and settings; `HistoricalScene.cs` composites animation over the three embedded PNG backgrounds. `Installer.cs` is shared per-user setup/uninstall. `Nightshift.cs`, `city.png`, and `nightshift.wav` preserve NIGHTSHIFT's original visuals and audio. The three historical builds contain only their own artwork and no audio dependencies or assets.

Screensaver switches: `/s` fullscreen, `/c` settings, `/p HWND` or `/p:HWND` embedded Windows preview, `/demo` windowed, `/render PATH SECONDS` deterministic PNG export. The renderer uses 640x360 pixels and nearest-neighbor scaling with letterboxing. Fullscreen supports each attached screen; input exits. Preview does not capture keys or mouse.

## Validation

Validated on the build machine on 2026-10-12: actual per-user installation, installed payload hashes, app registration, settings dialog, native `/p` parent embedding and parent-close exit, `/s` launch and key exit, installed uninstaller, removal of files/registration, and exact restoration of prior screensaver/idle/security registry values. See `validation.txt` and `verify.ps1`.

The lifecycle verification temporarily changes the current user's screensaver and then restores it; run it only when you are ready for brief fullscreen launches. Multi-monitor behavior and a real idle timeout were not observed end to end. Unsigned packages were not tested on a separate clean Windows computer. NIGHTSHIFT's soundtrack is retained; audio listening quality was not revalidated.

## Artwork and history

The three historical backgrounds were generated for CallyLab using the user's NIGHTSHIFT preview as a visual reference, then paired with original code-driven animation: deep indigo/violet nights, amber lights, cyan moonlight and broken reflections. Source PNGs are embedded at build time.

NIGHTSHIFT 1.0.1 changes packaging/setup only; its original renderer, artwork and soundtrack remain intact. BREACH is retired from current website offerings. Its September release remains available as a historical asset and has not been deleted.

## Motion update 1.1.0

Approved motion-study renderer: removed all procedural people, riders, carts, boats and sheep. Added moving cloud layers, stars/shooting star, animated fire/glows/embers, rolling smoke, drizzle/splashes, mist, fountain spray, foliage and stronger reflections. The three historical scenes remain entirely silent. NIGHTSHIFT unchanged.

